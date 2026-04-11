using FluentAssertions;
using Moq;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _bookingService = new BookingService(_eventServiceMock.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldReturnPendingBooking_WhenEventExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        // Act
        var booking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        booking.Should().NotBeNull();
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateMultipleBookingsWithUniqueIds_WhenCalledMultipleTimes()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var booking2 = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var booking3 = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        booking1.Id.Should().NotBe(booking2.Id);
        booking2.Id.Should().NotBe(booking3.Id);
        booking1.Id.Should().NotBe(booking3.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnCorrectBooking_WhenBookingExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var createdBooking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Act
        var retrievedBooking = await _bookingService.GetBookingByIdAsync(createdBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        retrievedBooking.Should().NotBeNull();
        retrievedBooking.Id.Should().Be(createdBooking.Id);
        retrievedBooking.EventId.Should().Be(createdBooking.EventId);
        retrievedBooking.Status.Should().Be(createdBooking.Status);
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldReflectStatusChange_WhenStatusIsUpdated()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var createdBooking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        
        var updatedBooking = new Booking 
        { 
            Id = createdBooking.Id, 
            EventId = createdBooking.EventId,
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = createdBooking.CreatedAt
        };

        // Act
        await _bookingService.UpdateBookingAsync(updatedBooking, TestContext.Current.CancellationToken);
        var retrievedBooking = await _bookingService.GetBookingByIdAsync(createdBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        retrievedBooking.Status.Should().Be(BookingStatus.Confirmed);
        retrievedBooking.ProcessedAt.Should().Be(updatedBooking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(eventId, "Событие не найдено."));

        // Act
        var action = () => _bookingService.CreateBookingAsync(eventId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventIsDeleted()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        // Simulating deleted event by throwing NotFoundException as the EventService would do
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(eventId, "Событие не найдено."));

        // Act
        var action = () => _bookingService.CreateBookingAsync(eventId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid();

        // Act
        var action = () => _bookingService.GetBookingByIdAsync(nonExistentBookingId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
    
    [Fact]
    public async Task GetPendingBookingsAsync_ShouldReturnOnlyPendingBookings()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var booking1 = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var booking2 = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var booking3 = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        
        var updatedBooking = new Booking 
        { 
            Id = booking2.Id, 
            EventId = booking2.EventId,
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = booking2.CreatedAt
        };
        await _bookingService.UpdateBookingAsync(updatedBooking, TestContext.Current.CancellationToken);

        // Act
        var pendingBookings = await _bookingService.GetPendingBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        var enumerable = pendingBookings.ToList();
        enumerable.Should().HaveCount(2);
        enumerable.Should().Contain(b => b.Id == booking1.Id);
        enumerable.Should().Contain(b => b.Id == booking3.Id);
        enumerable.Should().NotContain(b => b.Id == booking2.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats_WhenBookingIsCreated()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: 5);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        // Act
        await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        eventItem.AvailableSeats.Should().Be(4);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldSucceedForAllBookings_WhenCreatedUpToSeatsLimit()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        const int seats = 3;
        var eventItem = CreateTestEvent(eventId, totalSeats: seats);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        // Act
        var bookings = new List<Booking>();
        for (var i = 0; i < seats; i++)
            bookings.Add(await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken));

        // Assert
        bookings.Should().HaveCount(seats);
        bookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
        eventItem.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_AfterAllSeatsAreExhausted()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: 2);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Act
        var action = () => _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<NoAvailableSeatsException>();
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeatsAvailable()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: 5);
        eventItem.AvailableSeats = 0;
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        // Act
        var action = () => _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<NoAvailableSeatsException>();
    }

    [Fact]
    public async Task Confirm_ShouldSetStatusToConfirmedAndFillProcessedAt_WhenCalled()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var beforeConfirm = DateTime.UtcNow;

        // Act
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeOnOrAfter(beforeConfirm);
    }

    [Fact]
    public async Task Reject_ShouldSetStatusToRejectedAndFillProcessedAt_WhenCalled()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        var beforeReject = DateTime.UtcNow;

        // Act
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeOnOrAfter(beforeReject);
    }

    [Fact]
    public async Task ReleaseSeats_AfterReject_ShouldRestoreAvailableSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: 1);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        eventItem.AvailableSeats.Should().Be(0);

        // Act
        booking.Reject();
        eventItem.ReleaseSeats();

        // Assert
        eventItem.AvailableSeats.Should().Be(1);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldSucceed_AfterRejectAndReleaseSeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: 1);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var firstBooking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);
        firstBooking.Reject();
        eventItem.ReleaseSeats();

        // Act
        var secondBooking = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        secondBooking.Should().NotBeNull();
        secondBooking.Status.Should().Be(BookingStatus.Pending);
        eventItem.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldPreventOverbooking_UnderConcurrentRequests()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: totalSeats);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var successCount = 0;
        var noSeatsCount = 0;
        var ct = TestContext.Current.CancellationToken;

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            try
            {
                await _bookingService.CreateBookingAsync(eventId, ct);
                Interlocked.Increment(ref successCount);
            }
            catch (NoAvailableSeatsException)
            {
                Interlocked.Increment(ref noSeatsCount);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().Be(totalSeats);
        noSeatsCount.Should().Be(concurrentRequests - totalSeats);
        eventItem.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldGenerateUniqueIds_UnderConcurrentRequests()
    {
        // Arrange
        const int totalSeats = 10;
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(eventId, totalSeats: totalSeats);
        _eventServiceMock.Setup(s => s.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);

        var ct = TestContext.Current.CancellationToken;

        // Act
        var tasks = Enumerable.Range(0, totalSeats)
            .Select(_ => _bookingService.CreateBookingAsync(eventId, ct));

        var bookings = await Task.WhenAll(tasks);

        // Assert
        bookings.Should().HaveCount(totalSeats);
        bookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
    }

    private static Event CreateTestEvent(Guid eventId, int totalSeats = 10) =>
        new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Description",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddDays(1),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
}
