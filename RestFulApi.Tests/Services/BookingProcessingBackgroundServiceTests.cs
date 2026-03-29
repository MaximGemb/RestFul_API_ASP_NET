using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RestFulApi.Interfaces;
using RestFulApi.Models;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class BookingProcessingBackgroundServiceTests
{
    private readonly Mock<IBookingService> _bookingServiceMock;
    private readonly TestBookingProcessingBackgroundService _backgroundService;

    public BookingProcessingBackgroundServiceTests()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var serviceScopeMock = new Mock<IServiceScope>();
        _bookingServiceMock = new Mock<IBookingService>();
        var loggerMock = new Mock<ILogger<BookingProcessingBackgroundService>>();

        serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);

        serviceScopeFactoryMock.Setup(f => f.CreateScope())
            .Returns(serviceScopeMock.Object);

        serviceScopeMock.Setup(s => s.ServiceProvider)
            .Returns(serviceProviderMock.Object);

        serviceProviderMock.Setup(sp => sp.GetService(typeof(IBookingService)))
            .Returns(_bookingServiceMock.Object);

        _backgroundService = new TestBookingProcessingBackgroundService(
            serviceProviderMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExitImmediately_WhenTokenIsAlreadyCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await _backgroundService.ExposeExecuteAsync(cts.Token);

        // Assert
        // The service should just log start and stop, and not call GetPendingBookingsAsync
        _bookingServiceMock.Verify(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetPendingBookingsAsync_BeforeBeingCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
    
        _bookingServiceMock.Setup(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>())
            .Callback(() => cts.Cancel()); // Отменяем токен сразу после первого вызова

        // Act
        // Используем Record. ExceptionAsync, чтобы поймать TaskCanceledException и не дать тесту упасть
        var exception = await Record.ExceptionAsync(() => _backgroundService.ExposeExecuteAsync(cts.Token));

        // Assert
        // Проверяем, что если исключение и было, то это именно отмена (или вообще без ошибок)
        Assert.True(exception is null or OperationCanceledException or TaskCanceledException);
    
        _bookingServiceMock.Verify(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _bookingServiceMock.Verify(s => s.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task ExecuteAsync_ShouldProcessBooking_AndHandleCancellationDuringProcessing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var pendingBooking = new Booking { Id = Guid.NewGuid(), Status = BookingStatus.Pending };
        
        _bookingServiceMock.Setup(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { pendingBooking });

        _backgroundService.DelayAction = async (token) => 
        {
            await Task.Delay(1000, token);
        };

        // We can't easily test the UpdateBookingAsync because of the 1-minute delay, 
        // but we can ensure it tries to delay and handles cancellation.
        cts.CancelAfter(50); // Cancel shortly after starting

        // Act
        await _backgroundService.ExposeExecuteAsync(cts.Token);

        // Assert
        _bookingServiceMock.Verify(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        // Ensure it doesn't get to UpdateBookingAsync because of cancellation during Task.Delay
        _bookingServiceMock.Verify(s => s.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateBookingStatus_AndCallUpdateBookingAsync()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var pendingBooking = new Booking { Id = Guid.NewGuid(), Status = BookingStatus.Pending, ProcessedAt = null };
        
        _bookingServiceMock.Setup(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { pendingBooking })
            .Callback(() => cts.Cancel()); // Отменяем после получения, чтобы завершить цикл

        _backgroundService.DelayAction = (token) => Task.CompletedTask; // Мгновенное выполнение

        // Act
        var exception = await Record.ExceptionAsync(() => _backgroundService.ExposeExecuteAsync(cts.Token));

        // Assert
        Assert.True(exception is null or OperationCanceledException or TaskCanceledException);

        _bookingServiceMock.Verify(s => s.UpdateBookingAsync(It.Is<Booking>(b => 
            b.Id == pendingBooking.Id && 
            b.Status == BookingStatus.Confirmed && 
            b.ProcessedAt != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DelayProcessingAsync_ShouldDelay_AndHandleCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Отменяем сразу, чтобы тест не шел 1 минуту

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            _backgroundService.ExposeBaseDelayProcessingAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueRunning_WhenExceptionOccurs()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callCount = 0;

        _bookingServiceMock.Setup(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => 
            {
                callCount++;
                if (callCount is 1) throw new Exception("Test exception");
            
                cts.Cancel(); // Отменяем, чтобы выйти из цикла на второй итерации
                return new List<Booking>();
            });

        // Act
        // Ловим исключение отмены, чтобы тест не падал
        _ = await Record.ExceptionAsync(() => _backgroundService.ExposeExecuteAsync(cts.Token));

        // Assert
        // Проверяем, что сервис не «умер» после первой ошибки и вызвал метод второй раз
        _bookingServiceMock.Verify(s => s.GetPendingBookingsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }


    /// <summary>
    /// Вспомогательный класс для тестирования защищенного метода ExecuteAsync.
    /// </summary>
    private class TestBookingProcessingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BookingProcessingBackgroundService> logger)
        : BookingProcessingBackgroundService(serviceProvider, logger)
    {
        public Func<CancellationToken, Task> DelayAction { get; set; } = 
            (token) => Task.Delay(TimeSpan.FromMinutes(1), token);

        public Task ExposeExecuteAsync(CancellationToken stoppingToken) => 
            ExecuteAsync(stoppingToken);

        public Task ExposeBaseDelayProcessingAsync(CancellationToken stoppingToken) => 
            base.DelayProcessingAsync(stoppingToken);

        protected override Task DelayProcessingAsync(CancellationToken stoppingToken) => 
            DelayAction(stoppingToken);
    }
}
