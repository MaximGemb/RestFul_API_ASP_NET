using System.Text.Json;
using Confluent.Kafka;
using EventService.Application.Interfaces;
using EventService.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.BookingContracts;

namespace EventService.Infrastructure.Kafka;

/// <summary>
/// Фоновый сервис-подписчик на топик <see cref="BookingTopics.BOOKING_CONFIRMED"/>.
/// При получении сообщения уменьшает количество доступных мест у соответствующего события.
/// <para>
/// BackgroundService — singleton, поэтому DbContext и репозиторий создаются
/// через <see cref="IServiceScopeFactory"/> в рамках каждого сообщения.
/// </para>
/// <para>
/// Метод Consume у Kafka-клиента блокирующий — цикл чтения выполняется
/// в отдельном потоке через <see cref="Task.Run"/>.
/// </para>
/// </summary>
public sealed class BookingConfirmedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingConfirmedConsumer"/>.
    /// </summary>
    /// <param name="scopeFactory">Фабрика DI-скоупов для доступа к scoped-репозиторию.</param>
    /// <param name="options">Параметры Kafka из конфигурации.</param>
    /// <param name="logger">Логгер.</param>
    public BookingConfirmedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(BookingTopics.BOOKING_CONFIRMED);

        _logger.LogInformation(
            "BookingConfirmedConsumer подписан на топик {Topic}, группа {Group}.",
            BookingTopics.BOOKING_CONFIRMED, _options.ConsumerGroup);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;

                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка чтения сообщения из Kafka.");
                    continue;
                }

                if (result?.Message?.Value is null)
                    continue;

                BookingConfirmed? message;
                try
                {
                    message = JsonSerializer.Deserialize<BookingConfirmed>(result.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Не удалось десериализовать сообщение из Kafka. Offset={Offset}. Сообщение пропущено.",
                        result.Offset);
                    consumer.StoreOffset(result);
                    continue;
                }

                if (message is null)
                {
                    _logger.LogWarning(
                        "Получено пустое сообщение из Kafka. Offset={Offset}. Сообщение пропущено.",
                        result.Offset);
                    consumer.StoreOffset(result);
                    continue;
                }

                ProcessMessageAsync(message, stoppingToken).GetAwaiter().GetResult();
                consumer.StoreOffset(result);
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("BookingConfirmedConsumer остановлен.");
        }
    }

    private async Task ProcessMessageAsync(BookingConfirmed message, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

            if (await inboxRepository.ExistsAsync(message.BookingId, stoppingToken))
            {
                _logger.LogInformation(
                    "Бронь {BookingId} уже обработана (дублирующее сообщение). Пропускаем.",
                    message.BookingId);
                return;
            }

            var @event = await repository.FindByIdAsync(message.EventId, stoppingToken);

            if (@event is null)
            {
                _logger.LogWarning(
                    "Событие {EventId} не найдено. Бронь {BookingId} пропущена.",
                    message.EventId, message.BookingId);
                return;
            }

            try
            {
                @event.TryReserveSeats(message.SeatsCount);
            }
            catch (NoAvailableSeatsException)
            {
                _logger.LogWarning(
                    "Нет свободных мест для события {EventId} (запрошено: {SeatsCount}). Бронь {BookingId} пропущена.",
                    message.EventId, message.SeatsCount, message.BookingId);
                return;
            }

            inboxRepository.Add(message.BookingId, nameof(BookingConfirmed));

            await repository.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Места события {EventId} уменьшены на {SeatsCount}. Бронь {BookingId} обработана.",
                message.EventId, message.SeatsCount, message.BookingId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка обработки BookingConfirmed для брони {BookingId}, события {EventId}.",
                message.BookingId, message.EventId);
        }
    }
}
