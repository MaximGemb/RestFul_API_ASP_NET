using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestFulApi.Controllers;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;
using Xunit;

namespace RestFulApi.Tests.Controllers;

public class BookingsControllerTests
{
    private readonly Mock<IBookingService> _bookingServiceMock;
    private readonly BookingsController _controller;

    public BookingsControllerTests()
    {
        _bookingServiceMock = new Mock<IBookingService>();
        _controller = new BookingsController(_bookingServiceMock.Object);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnOkResult_WithBooking_WhenBookingExists()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _bookingServiceMock.Setup(s => s.GetBookingByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var cts = new CancellationTokenSource();

        // Act
        var actionResult = await _controller.GetBooking(bookingId, cts.Token);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBooking = okResult.Value.Should().BeOfType<Booking>().Subject;
        
        returnedBooking.Id.Should().Be(bookingId);
        returnedBooking.EventId.Should().Be(booking.EventId);
        returnedBooking.Status.Should().Be(booking.Status);
    }

    [Fact]
    public async Task GetBooking_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _bookingServiceMock.Setup(s => s.GetBookingByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(bookingId, "Бронь не найдена."));

        var cts = new CancellationTokenSource();

        // Act
        var action = () => _controller.GetBooking(bookingId, cts.Token);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
