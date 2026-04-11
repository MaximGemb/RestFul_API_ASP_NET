using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований со статусом Pending.
/// </summary>
public class BookingProcessingBackgroundService : BackgroundService
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingProcessingBackgroundService"/>.
    /// </summary>
    /// <param name="bookingService">Провайдер сервисов для создания области видимости (Scope).</param>
    /// <param name="logger">Логгер.</param>
    // ReSharper disable once MemberCanBeProtected.Global
    public BookingProcessingBackgroundService(
        IBookingService bookingService,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Основной цикл фоновой задачи.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены для остановки сервиса.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingProcessingBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                var pendingBookings = await _bookingService.GetPendingBookingsAsync(stoppingToken);

                foreach (var booking in pendingBookings)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    // Имитация долгой обработки внешним сервисом
                    await DelayProcessingAsync(stoppingToken);

                    // Обновляем статус и время обработки
                    booking.Status = Random.Shared.Next(2) is 0 
                        ? BookingStatus.Confirmed 
                        : BookingStatus.Rejected;
                    booking.ProcessedAt = DateTime.UtcNow;

                    await _bookingService.UpdateBookingAsync(booking, stoppingToken);
                }
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
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("BookingProcessingBackgroundService остановлен");
    }

    /// <summary>
    /// Имитирует долгую обработку.
    /// </summary>
    protected virtual Task DelayProcessingAsync(CancellationToken stoppingToken) => 
        Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
}
