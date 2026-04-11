using System.ComponentModel.DataAnnotations;
using RestFulApi.Models;
using Xunit;

namespace RestFulApi.Tests.Models;

public class EventTests
{
    [Fact]
    public void Validate_ShouldReturnError_WhenStartAtIsAfterEndAt()
    {
        // Arrange
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            TotalSeats = 1,
            StartAt = new DateTime(2023, 1, 2, 10, 0, 0),
            EndAt = new DateTime(2023, 1, 1, 10, 0, 0)
        };
        var validationContext = new ValidationContext(@event);

        // Act
        var results = @event.Validate(validationContext).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("Дата завершения должна быть позже даты начала.", results[0].ErrorMessage);
        Assert.Contains("StartAt", results[0].MemberNames);
        Assert.Contains("EndAt", results[0].MemberNames);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenStartAtIsEqualToEndAt()
    {
        // Arrange
        var date = new DateTime(2023, 1, 1, 10, 0, 0);
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            TotalSeats = 1,
            StartAt = date,
            EndAt = date
        };
        var validationContext = new ValidationContext(@event);

        // Act
        var results = @event.Validate(validationContext).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("Дата завершения должна быть позже даты начала.", results[0].ErrorMessage);
    }

    [Fact]
    public void Validate_ShouldNotReturnError_WhenStartAtIsBeforeEndAt()
    {
        // Arrange
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            TotalSeats = 1,
            StartAt = new DateTime(2023, 1, 1, 10, 0, 0),
            EndAt = new DateTime(2023, 1, 2, 10, 0, 0)
        };
        var validationContext = new ValidationContext(@event);

        // Act
        var results = @event.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }
}
