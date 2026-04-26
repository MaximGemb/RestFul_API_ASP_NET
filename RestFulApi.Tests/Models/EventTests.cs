using FluentAssertions;
using RestFulApi.Exceptions;
using RestFulApi.Models;
using Xunit;

namespace RestFulApi.Tests.Models;

public class EventTests
{
    [Fact]
    public void Create_ShouldThrowValidationException_WhenTitleIsEmpty()
    {
        var now = DateTime.UtcNow;

        var action = () => Event.Create(
            title: "   ",
            startAt: now.AddDays(1),
            endAt: now.AddDays(2),
            totalSeats: 1,
            description: "desc");

        var exception = action.Should().Throw<CustomValidationException>().Which;
        exception.Errors.Should().ContainKey("Title");
        exception.Errors["Title"].Should().Contain("Title cannot be empty");
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenStartAtIsNull()
    {
        var now = DateTime.UtcNow;

        var action = () => Event.Create(
            title: "Test",
            startAt: null,
            endAt: now.AddDays(2),
            totalSeats: 1,
            description: "desc");

        var exception = action.Should().Throw<CustomValidationException>().Which;
        exception.Errors.Should().ContainKey("StartAt");
        exception.Errors["StartAt"].Should().Contain("Start time cannot be null");
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenEndAtIsNull()
    {
        var now = DateTime.UtcNow;

        var action = () => Event.Create(
            title: "Test",
            startAt: now.AddDays(1),
            endAt: null,
            totalSeats: 1,
            description: "desc");

        var exception = action.Should().Throw<CustomValidationException>().Which;
        exception.Errors.Should().ContainKey("EndAt");
        exception.Errors["EndAt"].Should().Contain("End time cannot be null");
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenStartAtIsInPast()
    {
        var now = DateTime.UtcNow;

        var action = () => Event.Create(
            title: "Test",
            startAt: now.AddMinutes(-1),
            endAt: now.AddDays(1),
            totalSeats: 1,
            description: "desc");

        var exception = action.Should().Throw<CustomValidationException>().Which;
        exception.Errors.Should().ContainKey("StartAt");
        exception.Errors["StartAt"].Should().Contain("Event cannot start in the past");
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenEndIsEarlierThanStart()
    {
        var now = DateTime.UtcNow;

        var action = () => Event.Create(
            title: "Test",
            startAt: now.AddDays(2),
            endAt: now.AddDays(1),
            totalSeats: 1,
            description: "desc");

        action.Should().Throw<CustomValidationException>();
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenTotalSeatsNotPositive()
    {
        var now = DateTime.UtcNow;

        var action = () => Event.Create(
            title: "Test",
            startAt: now.AddDays(1),
            endAt: now.AddDays(2),
            totalSeats: 0,
            description: "desc");

        action.Should().Throw<CustomValidationException>();
    }

    [Fact]
    public void TryReserveSeats_ShouldDecreaseAvailableSeats()
    {
        var @event = CreateValidEvent(totalSeats: 3);

        @event.TryReserveSeats();

        @event.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public void TryReserveSeats_ShouldThrowNoAvailableSeats_WhenSeatsEnded()
    {
        var @event = CreateValidEvent(totalSeats: 1);
        @event.TryReserveSeats();

        var action = () => @event.TryReserveSeats();

        action.Should().Throw<NoAvailableSeatsException>();
    }

    [Fact]
    public void ReleaseSeats_ShouldNotExceedTotalSeats()
    {
        var @event = CreateValidEvent(totalSeats: 2);
        @event.TryReserveSeats(2);

        @event.ReleaseSeats(3);

        @event.AvailableSeats.Should().Be(2);
    }

    private static Event CreateValidEvent(int totalSeats = 5)
    {
        var now = DateTime.UtcNow;
        return Event.Create(
            title: "Event",
            startAt: now.AddDays(1),
            endAt: now.AddDays(2),
            totalSeats: totalSeats,
            description: "desc");
    }
}
