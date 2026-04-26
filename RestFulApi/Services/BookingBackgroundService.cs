using Microsoft.EntityFrameworkCore;
using RestFulApi.DataAccess;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Фоновый сервис, отвечающий за обработку бронирований со статусом Pending.
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
    /// Логгер, используемый для записи информации, предупреждений и ошибок, возникающих при работе
    /// фонового сервиса обработки бронирований.
    /// </summary>
    private readonly ILogger<BookingBackgroundService> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр фонового сервиса для обработки бронирований со статусом Pending.
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
    /// <returns>Задача, представляющая выполнение асинхронной операции.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingProcessingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendingBookingIds;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    pendingBookingIds = await context.Bookings
                        .Where(b => b.Status == BookingStatus.Pending)
                        .Select(b => b.Id)
                        .ToListAsync(stoppingToken);
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

            // Задержка перед следующим циклом опроса
            await DelayPollingAsync(stoppingToken);
        }

        _logger.LogInformation("BookingProcessingBackgroundService остановлен");
    }

    /// <summary>
    /// Обрабатывает указанное бронирование, обновляя его статус в зависимости от доступности события.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования для обработки.</param>
    /// <param name="stoppingToken">Токен для уведомления об отмене операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки бронирования.</returns>
    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await DelayProcessingAsync(stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
            if (booking is not { Status: BookingStatus.Pending })
                return;

            var @event = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);
            if (@event is null)
            {
                booking.Reject();
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogWarning("Событие {Id} не найдено. Бронь {BId} отклонена.", booking.EventId, booking.Id);
                return;
            }

            booking.Confirm();
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Бронь {BId} для события {Id} обработана → {Status}",
                booking.Id, booking.EventId, booking.Status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
                if (booking != null)
                {
                    booking.Reject();

                    var @event = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);
                    @event?.ReleaseSeats();

                    await context.SaveChangesAsync(stoppingToken);
                }

                _logger.LogError(ex, "Ошибка брони {Id}.", bookingId);
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx, "Не удалось отклонить бронь {Id} после ошибки.", bookingId);
            }
        }
    }

    /// <summary>
    /// Выполняет имитацию задержки при обработке.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены, используемый для завершения операции задержки.</param>
    /// <returns>Задача, представляющая асинхронную операцию задержки.</returns>
    protected virtual Task DelayProcessingAsync(CancellationToken stoppingToken) =>
        Task.Delay(ProcessingDelay, stoppingToken);

    /// <summary>
    /// Выполняет задержку между циклами опроса базы данных.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены, используемый для завершения операции задержки.</param>
    /// <returns>Задача, представляющая асинхронную операцию задержки.</returns>
    protected virtual Task DelayPollingAsync(CancellationToken stoppingToken) =>
        Task.Delay(PollingInterval, stoppingToken);
}