using System.Text.Json;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.BookingContracts;

namespace BookingService.Infrastructure.Kafka;

/// <summary>
/// Фоновый relay-сервис паттерна Outbox.
/// Опрашивает таблицу <c>outbox_messages</c>, публикует необработанные сообщения
/// в Kafka и помечает их как отправленные.
/// <para>
/// Гарантирует, что каждое сообщение будет опубликовано хотя бы один раз (at-least-once):
/// если приложение упадёт после публикации, но до сохранения <see cref="OutboxMessage.ProcessedAt"/>,
/// сообщение будет отправлено повторно при следующем запуске.
/// </para>
/// </summary>
public sealed class OutboxRelayService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OutboxRelayService> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="OutboxRelayService"/>.
    /// </summary>
    /// <param name="scopeFactory">Фабрика DI-скоупов для доступа к scoped-репозиторию.</param>
    /// <param name="eventPublisher">Издатель событий (singleton, потокобезопасен).</param>
    /// <param name="logger">Логгер.</param>
    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        IEventPublisher eventPublisher,
        ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Основной цикл relay-сервиса.
    /// </summary>
    /// <param name="stoppingToken">Токен для остановки фоновой задачи.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxRelayService запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке Outbox-сообщений.");
            }

            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("OutboxRelayService остановлен.");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var messages = await outboxRepository.GetUnprocessedAsync(stoppingToken);

        foreach (var message in messages)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await RelayMessageAsync(message, outboxRepository, stoppingToken);
        }
    }

    private async Task RelayMessageAsync(
        OutboxMessage message,
        IOutboxRepository outboxRepository,
        CancellationToken stoppingToken)
    {
        try
        {
            if (message.Type == nameof(BookingConfirmed))
            {
                var confirmed = JsonSerializer.Deserialize<BookingConfirmed>(message.Payload)!;
                await _eventPublisher.PublishBookingConfirmedAsync(confirmed, stoppingToken);
            }
            else
            {
                _logger.LogWarning(
                    "Неизвестный тип Outbox-сообщения: {Type}. Id={Id}. Сообщение помечено как обработанное.",
                    message.Type, message.Id);
            }

            message.MarkAsProcessed();
            await outboxRepository.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Outbox-сообщение {Id} ({Type}) успешно опубликовано в Kafka.",
                message.Id, message.Type);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ошибка публикации Outbox-сообщения {Id} ({Type}). Будет повторная попытка.",
                message.Id, message.Type);
        }
    }
}
