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
        var eventItem = new Event { Id = eventId, Title = "Test Event", Description = "Description", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddDays(1), TotalSeats = 10, AvailableSeats = 10 };
        
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
        var eventItem = new Event { Id = eventId, Title = "Test Event", Description = "Description", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddDays(1), TotalSeats = 10, AvailableSeats = 10 };
        
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
        var eventItem = new Event { Id = eventId, Title = "Test Event", Description = "Description", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddDays(1), TotalSeats = 10, AvailableSeats = 10 };
        
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
        var eventItem = new Event { Id = eventId, Title = "Test Event", Description = "Description", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddDays(1), TotalSeats = 10, AvailableSeats = 10 };
        
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
        var eventItem = new Event { Id = eventId, Title = "Test Event", Description = "Description", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddDays(1), TotalSeats = 10, AvailableSeats = 10 };
        
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
}
