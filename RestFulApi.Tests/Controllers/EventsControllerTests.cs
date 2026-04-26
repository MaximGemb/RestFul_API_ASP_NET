using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Moq;
using RestFulApi.Controllers;
using RestFulApi.DTOs;
using RestFulApi.Interfaces;
using RestFulApi.Models;
using Xunit;

namespace RestFulApi.Tests.Controllers;

public class EventsControllerTests
{
    [Fact]
    public async Task GetEvents_ShouldReturnOkObjectResult_WithPaginatedEvents()
    {
        // Arrange
        var serviceMock = new Mock<IEventService>();
        var expected = new PaginatedResult<EventInfo>
        {
            TotalCount = 1,
            Page = 1,
            PageSize = 10,
            Items =
            [
                new EventInfo
                {
                    Id = Guid.NewGuid(),
                    Title = "Conference",
                    Description = "Description",
                    StartAt = new DateTime(2026, 01, 01),
                    EndAt = new DateTime(2026, 01, 02),
                    TotalSeats = 100,
                    AvailableSeats = 100
                }
            ]
        };

        serviceMock
            .Setup(service => service.GetAllEventsAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new EventsController(serviceMock.Object, new Mock<IBookingService>().Object);
        var cts = new CancellationTokenSource();

        // Act
        var actionResult = await controller.GetEvents(ct: cts.Token);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeOfType<PaginatedResult<EventInfo>>().Subject;
        value.TotalCount.Should().Be(expected.TotalCount);
        value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetEvent_ShouldReturnOkObjectResult_WhenEventExists()
    {
        // Arrange
        var serviceMock = new Mock<IEventService>();
        var eventId = Guid.NewGuid();
        var expected = new EventInfo
        {
            Id = eventId,
            Title = "Conference",
            Description = "Description",
            StartAt = new DateTime(2026, 02, 01),
            EndAt = new DateTime(2026, 02, 02),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        serviceMock
            .Setup(service => service.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new EventsController(serviceMock.Object, new Mock<IBookingService>().Object);
        var cts = new CancellationTokenSource();

        // Act
        var actionResult = await controller.GetEvent(eventId, cts.Token);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeOfType<EventInfo>().Subject;
        value.Id.Should().Be(eventId);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnCreatedAtAction_WhenEventIsCreated()
    {
        // Arrange
        var serviceMock = new Mock<IEventService>();
        var dto = new CreateEvent
        {
            Title = "Conference",
            Description = "Description",
            StartAt = new DateTime(2026, 03, 01),
            EndAt = new DateTime(2026, 03, 02)
        };
        var created = new EventInfo
        {
            Id = Guid.NewGuid(),
            Title = dto.Title!,
            Description = dto.Description,
            StartAt = dto.StartAt!.Value,
            EndAt = dto.EndAt!.Value,
            TotalSeats = 0,
            AvailableSeats = 0
        };

        serviceMock
            .Setup(service => service.CreateEventAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var controller = new EventsController(serviceMock.Object, new Mock<IBookingService>().Object);
        var cts = new CancellationTokenSource();

        // Act
        var actionResult = await controller.CreateEvent(dto, cts.Token);

        // Assert
        var createdResult = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(EventsController.GetEvent));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
        var value = createdResult.Value.Should().BeOfType<EventInfo>().Subject;
        value.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task UpdateEvent_ShouldReturnNoContent_WhenEventIsUpdated()
    {
        // Arrange
        var serviceMock = new Mock<IEventService>();
        var eventId = Guid.NewGuid();
        var dto = new UpdateEvent
        {
            Title = "Updated",
            Description = "Description",
            StartAt = new DateTime(2026, 04, 01),
            EndAt = new DateTime(2026, 04, 02)
        };

        serviceMock
            .Setup(service => service.UpdateEventAsync(eventId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventInfo
            {
                Id = eventId,
                Title = dto.Title!,
                Description = dto.Description,
                StartAt = dto.StartAt!.Value,
                EndAt = dto.EndAt!.Value,
                TotalSeats = 100,
                AvailableSeats = 100
            });

        var controller = new EventsController(serviceMock.Object, new Mock<IBookingService>().Object);
        var cts = new CancellationTokenSource();

        // Act
        var result = await controller.UpdateEvent(eventId, dto, cts.Token);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteEvent_ShouldReturnNoContent_WhenEventIsDeleted()
    {
        // Arrange
        var serviceMock = new Mock<IEventService>();
        var eventId = Guid.NewGuid();

        serviceMock
            .Setup(service => service.DeleteEventAsync(eventId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new EventsController(serviceMock.Object, new Mock<IBookingService>().Object);
        var cts = new CancellationTokenSource();

        // Act
        var result = await controller.DeleteEvent(eventId, cts.Token);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task BookEvent_ShouldReturnAcceptedAtAction_WhenBookingIsCreated()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = new BookingInfo
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var bookingServiceMock = new Mock<IBookingService>();
        bookingServiceMock
            .Setup(service => service.CreateBookingAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var controller = new EventsController(new Mock<IEventService>().Object, bookingServiceMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        var result = await controller.BookEvent(eventId, cts.Token);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        acceptedResult.ActionName.Should().Be("GetBooking");
        acceptedResult.ControllerName.Should().Be("Bookings");
        acceptedResult.RouteValues!["id"].Should().Be(booking.Id);
        var value = acceptedResult.Value.Should().BeOfType<BookingInfo>().Subject;
        value.Id.Should().Be(booking.Id);
    }
}