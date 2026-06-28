using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Services;

/// <summary>
/// Фоновый сервис, отвечающий за обработку бронирований со статусом Pending.
/// Для каждого Pending-бронирования проверяет существование события в EventService
/// и подтверждает или отклоняет бронь.
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
    /// Инициализирует новый экземпляр фонового сервиса.
    /// </summary>
    /// <param name="scopeFactory">Фабрика DI-скоупов для доступа к scoped-сервисам.</param>
    /// <param name="logger">Логгер для записи информации о работе сервиса.</param>
    // ReSharper disable once MemberCanBeProtected.Global
    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
    /// Обрабатывает указанное бронирование: запрашивает EventService и подтверждает или отклоняет бронь.
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
            var eventServiceClient = scope.ServiceProvider.GetRequiredService<IEventServiceClient>();

            var booking = await bookingRepository.FindByIdAsync(bookingId, stoppingToken);
            if (booking is not { Status: BookingStatus.Pending })
                return;

            var eventInfo = await eventServiceClient.GetEventAvailabilityAsync(booking.EventId, stoppingToken);
            if (eventInfo is null)
            {
                booking.Reject();
                await bookingRepository.SaveChangesAsync(stoppingToken);

                _logger.LogWarning("Событие {EventId} не найдено в EventService. Бронь {BookingId} отклонена.",
                    booking.EventId, booking.Id);
                return;
            }

            booking.Confirm();
            await bookingRepository.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Бронь {BookingId} для события {EventId} подтверждена.",
                booking.Id, booking.EventId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventServiceClient = scope.ServiceProvider.GetRequiredService<IEventServiceClient>();

                var booking = await bookingRepository.FindByIdAsync(bookingId, stoppingToken);
                if (booking != null)
                {
                    booking.Reject();

                    try
                    {
                        await eventServiceClient.ReleaseSeatAsync(booking.EventId, stoppingToken);
                    }
                    catch (Exception releaseEx)
                    {
                        _logger.LogWarning(releaseEx, "Не удалось освободить место в EventService для брони {Id}.", bookingId);
                    }

                    await bookingRepository.SaveChangesAsync(stoppingToken);
                }

                _logger.LogError(ex, "Ошибка обработки брони {Id}.", bookingId);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Не удалось отклонить бронь {Id} после ошибки.", bookingId);
            }
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
