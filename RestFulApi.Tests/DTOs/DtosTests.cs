using FluentAssertions;
using RestFulApi.DTOs;
using RestFulApi.Models;
using Xunit;

namespace RestFulApi.Tests.DTOs;

public class DtosTests
{
    [Fact]
    public void CreateEvent_ShouldStoreProvidedValues()
    {
        var startAt = DateTime.UtcNow.AddDays(1);
        var endAt = startAt.AddHours(2);

        var dto = new CreateEvent
        {
            Title = "Architecture meetup",
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = 120,
            Description = "System design and scaling"
        };

        dto.Title.Should().Be("Architecture meetup");
        dto.StartAt.Should().Be(startAt);
        dto.EndAt.Should().Be(endAt);
        dto.TotalSeats.Should().Be(120);
        dto.Description.Should().Be("System design and scaling");
    }

    [Fact]
    public void UpdateEvent_ShouldStoreProvidedValues()
    {
        var startAt = DateTime.UtcNow.AddDays(2);
        var endAt = startAt.AddHours(3);

        var dto = new UpdateEvent
        {
            Title = "Updated title",
            StartAt = startAt,
            EndAt = endAt,
            Description = "Updated description"
        };

        dto.Title.Should().Be("Updated title");
        dto.StartAt.Should().Be(startAt);
        dto.EndAt.Should().Be(endAt);
        dto.Description.Should().Be("Updated description");
    }

    [Fact]
    public void EventInfo_ShouldStoreRequiredAndOptionalValues()
    {
        var id = Guid.NewGuid();
        var startAt = DateTime.UtcNow.AddDays(4);
        var endAt = startAt.AddHours(4);

        var dto = new EventInfo
        {
            Id = id,
            Title = "Product launch",
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = 250,
            AvailableSeats = 180,
            Description = "Main stage"
        };

        dto.Id.Should().Be(id);
        dto.Title.Should().Be("Product launch");
        dto.StartAt.Should().Be(startAt);
        dto.EndAt.Should().Be(endAt);
        dto.TotalSeats.Should().Be(250);
        dto.AvailableSeats.Should().Be(180);
        dto.Description.Should().Be("Main stage");
    }

    [Fact]
    public void BookingInfo_ShouldStoreRequiredAndOptionalValues()
    {
        var id = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var processedAt = createdAt.AddMinutes(1);

        var dto = new BookingInfo
        {
            Id = id,
            EventId = eventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = createdAt,
            ProcessedAt = processedAt
        };

        dto.Id.Should().Be(id);
        dto.EventId.Should().Be(eventId);
        dto.Status.Should().Be(BookingStatus.Confirmed);
        dto.CreatedAt.Should().Be(createdAt);
        dto.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public void PaginationRequest_ShouldHaveDefaultValues()
    {
        var request = new PaginationRequest();

        request.Page.Should().Be(1);
        request.PageSize.Should().Be(10);
    }

    [Fact]
    public void PaginationRequest_ShouldAllowCustomValues()
    {
        var request = new PaginationRequest
        {
            Page = 3,
            PageSize = 25
        };

        request.Page.Should().Be(3);
        request.PageSize.Should().Be(25);
    }

    [Fact]
    public void PaginatedResult_TotalPages_ShouldRoundUp()
    {
        var result = new PaginatedResult<int>
        {
            Items = [1, 2],
            TotalCount = 11,
            Page = 1,
            PageSize = 5
        };

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PaginatedResult_TotalPages_ShouldReturnExactPageCount()
    {
        var result = new PaginatedResult<string>
        {
            Items = ["a", "b"],
            TotalCount = 20,
            Page = 2,
            PageSize = 10
        };

        result.TotalPages.Should().Be(2);
    }
}
