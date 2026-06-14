using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Application.DTOs;
using Domain.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using System.Security.Claims;
using Xunit;

namespace Presentation.Tests.Controllers;

public class BookingsControllerTests
{
    private readonly Mock<IBookingService> _bookingServiceMock;
    private readonly BookingsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public BookingsControllerTests()
    {
        _bookingServiceMock = new Mock<IBookingService>();
        _controller = new BookingsController(_bookingServiceMock.Object);
        SetupControllerUser(isAdmin: false);
    }

    private void SetupControllerUser(bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new("sub", _userId.ToString()),
            new("role", isAdmin ? nameof(Roles.Admin) : nameof(Roles.User))
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetBooking_ShouldReturnOkResult_WithBooking_WhenBookingExists()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new BookingInfo
        {
            Id = bookingId,
            EventId = Guid.NewGuid(),
            UserId = _userId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _bookingServiceMock
            .Setup(s => s.GetBookingByIdAsync(bookingId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var cts = new CancellationTokenSource();

        // Act
        var actionResult = await _controller.GetBooking(bookingId, cts.Token);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBooking = okResult.Value.Should().BeOfType<BookingInfo>().Subject;

        returnedBooking.Id.Should().Be(bookingId);
        returnedBooking.EventId.Should().Be(booking.EventId);
        returnedBooking.Status.Should().Be(booking.Status);
    }

    [Fact]
    public async Task GetBooking_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _bookingServiceMock
            .Setup(s => s.GetBookingByIdAsync(bookingId, _userId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(bookingId, "Бронь не найдена."));

        var cts = new CancellationTokenSource();

        // Act
        var action = () => _controller.GetBooking(bookingId, cts.Token);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnUnauthorized_WhenSubClaimIsMissing()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var actionResult = await _controller.GetBooking(Guid.NewGuid(), CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CancelBooking_ShouldReturnOkResult_WhenBookingIsCancelledByOwner()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new BookingInfo
        {
            Id = bookingId,
            EventId = Guid.NewGuid(),
            UserId = _userId,
            Status = BookingStatus.Cancelled,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };

        _bookingServiceMock
            .Setup(s => s.CancelBookingAsync(bookingId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var cts = new CancellationTokenSource();

        // Act
        var actionResult = await _controller.CancelBooking(bookingId, cts.Token);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBooking = okResult.Value.Should().BeOfType<BookingInfo>().Subject;

        returnedBooking.Id.Should().Be(bookingId);
        returnedBooking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBooking_ShouldReturnUnauthorized_WhenSubClaimIsMissing()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var actionResult = await _controller.CancelBooking(Guid.NewGuid(), CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<UnauthorizedResult>();
    }
}
