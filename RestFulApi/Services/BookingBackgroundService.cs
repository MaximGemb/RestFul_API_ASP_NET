using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Фоновый сервис, отвечающий за обработку бронирований со статусом Pending.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    /// <summary>
    /// Предоставляет доступ к функциональности бронирования, включая создание,
    /// получение, обновление и обработку бронирований.
    /// Этот сервис отвечает за управление операциями, связанными с бронированием,
    /// такими как получение ожидающих бронирований, обеспечение согласованности при обновлении бронирований
    /// и взаимодействие с хранилищем данных для бронирований.
    /// </summary>
    private readonly IBookingService _bookingService;

    /// <summary>
    /// Сервис для работы со связанной функциональностью событий.
    /// Используется для получения, обновления, создания и удаления событий,
    /// а также для выполнения операций, связанных с событием, таких как получение событий по идентификатору.
    /// </summary>
    private readonly IEventService _eventService;

    /// <summary>
    /// Логгер, используемый для записи информации, предупреждений и ошибок, возникающих при работе
    /// фонового сервиса обработки бронирований.
    /// </summary>
    private readonly ILogger<BookingBackgroundService> _logger;

    /// <summary>
    /// Семафор, используемый для управления конкурентным доступом к процессу обработки бронирований,
    /// обеспечивающий обработку только одного бронирования за раз для поддержания потокобезопасности.
    /// </summary>
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    /// <summary>
    /// Инициализирует новый экземпляр фонового сервиса для обработки бронирований со статусом Pending.
    /// </summary>
    /// <param name="bookingService">Сервис для работы с бронированиями.</param>
    /// <param name="eventService">Сервис для работы с событиями.</param>
    /// <param name="logger">Логгер для записи информации о работе сервиса.</param>
    // ReSharper disable once MemberCanBeProtected.Global
    public BookingBackgroundService(
        IBookingService bookingService,
        IEventService eventService,
        ILogger<BookingBackgroundService> logger)
    {
        _bookingService = bookingService;
        _eventService = eventService;
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
                var pendingBookings = await _bookingService.GetPendingBookingsAsync(stoppingToken);

                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
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

            try
            {
                // Задержка перед следующим циклом опроса
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("BookingProcessingBackgroundService остановлен");
    }

    /// <summary>
    /// Обрабатывает указанное бронирование, обновляя его статус в зависимости от доступности события.
    /// </summary>
    /// <param name="booking">Бронирование, которое требуется обработать.</param>
    /// <param name="stoppingToken">Токен для уведомления об отмене операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки бронирования.</returns>
    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await DelayProcessingAsync(stoppingToken);

        await _processingSemaphore.WaitAsync(stoppingToken);
        try
        {
            Event? @event = null;
            try
            {
                @event = await _eventService.GetByIdAsync(booking.EventId, stoppingToken);
                booking.Confirm();
            }
            catch (NotFoundException)
            {
                _logger.LogWarning("Событие {Id} не найдено. Бронь {BId} отклонена.", booking.EventId, booking.Id);
                booking.Reject();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ошибка брони {Id}.", booking.Id);
                booking.Reject();
                @event?.ReleaseSeats();
            }

            await _bookingService.UpdateBookingAsync(booking, stoppingToken);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    /// <summary>
    /// Выполняет имитацию задержки при обработке.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены, используемый для завершения операции задержки.</param>
    /// <returns>Задача, представляющая асинхронную операцию задержки.</returns>
    protected virtual Task DelayProcessingAsync(CancellationToken stoppingToken) =>
        Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
}