using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.BookingContracts;

namespace BookingService.Application.Services;

/// <summary>
/// Фоновый сервис, отвечающий за обработку бронирований со статусом Pending.
/// Подтверждает бронь, сохраняет статус в БД и публикует событие BookingConfirmed в Kafka.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Фабрика для создания DI-скоупов при работе с DbContext из фонового сервиса.
    /// </summary>
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Логгер, используемый для записи информации, предупреждений и ошибок.
    /// </summary>
    private readonly ILogger<BookingBackgroundService> _logger;

    /// <summary>
    /// Издатель событий брони (singleton, потокобезопасен).
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// Инициализирует новый экземпляр фонового сервиса.
    /// </summary>
    /// <param name="scopeFactory">Фабрика DI-скоупов для доступа к scoped-сервисам.</param>
    /// <param name="logger">Логгер для записи информации о работе сервиса.</param>
    /// <param name="eventPublisher">Издатель событий брони.</param>
    // ReSharper disable once MemberCanBeProtected.Global
    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger,
        IEventPublisher eventPublisher)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Основной цикл фоновой задачи.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены для остановки фоновой задачи.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendingBookingIds;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    pendingBookingIds = await bookingRepository.GetPendingIdsAsync(stoppingToken);
                }

                var tasks = pendingBookingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке бронирований в фоновом режиме");
            }

            await DelayPollingAsync(stoppingToken);
        }

        _logger.LogInformation("BookingBackgroundService остановлен");
    }

    /// <summary>
    /// Подтверждает бронь, сохраняет статус в БД и публикует событие BookingConfirmed в Kafka.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования для обработки.</param>
    /// <param name="stoppingToken">Токен для уведомления об отмене операции.</param>
    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await DelayProcessingAsync(stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var booking = await bookingRepository.FindByIdAsync(bookingId, stoppingToken);
            if (booking is not { Status: BookingStatus.Pending })
                return;

            booking.Confirm();
            await bookingRepository.SaveChangesAsync(stoppingToken);

            var confirmedEvent = new BookingConfirmed(
                BookingId: booking.Id,
                EventId: booking.EventId,
                UserId: booking.UserId,
                SeatsCount: 1,
                ConfirmedAt: booking.ProcessedAt!.Value);

            await _eventPublisher.PublishBookingConfirmedAsync(confirmedEvent, stoppingToken);

            _logger.LogInformation("Бронь {BookingId} для события {EventId} подтверждена.",
                booking.Id, booking.EventId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки брони {Id}.", bookingId);
        }
    }

    /// <summary>
    /// Выполняет имитацию задержки при обработке.
    /// </summary>
    protected virtual Task DelayProcessingAsync(CancellationToken stoppingToken) =>
        Task.Delay(ProcessingDelay, stoppingToken);

    /// <summary>
    /// Выполняет задержку между циклами опроса базы данных.
    /// </summary>
    protected virtual Task DelayPollingAsync(CancellationToken stoppingToken) =>
        Task.Delay(PollingInterval, stoppingToken);
}
