using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RestFulApi.DTOs;
using RestFulApi.Exceptions;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class EventServiceTests
{
    [Fact]
    public async Task Create_ShouldCreateEvent_WhenDataIsValid()
    {
        // Arrange
        var service = new EventService();
        var dto = CreateEventDto("Tech Conference", new DateTime(2026, 04, 10, 9, 0, 0),
            new DateTime(2026, 04, 10, 18, 0, 0));
        var cts = new CancellationTokenSource();

        // Act
        var createdEvent = await service.CreateAsync(dto, cts.Token);

        // Assert
        createdEvent.Id.Should().NotBe(Guid.Empty);
        createdEvent.Title.Should().Be(dto.Title);
        createdEvent.Description.Should().Be(dto.Description);
        createdEvent.StartAt.Should().Be(dto.StartAt);
        createdEvent.EndAt.Should().Be(dto.EndAt);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllEvents_WhenEventsExist()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Alpha Meetup", new DateTime(2026, 04, 01), new DateTime(2026, 04, 02)), cts.Token);
        await service.CreateAsync(CreateEventDto("Beta Meetup", new DateTime(2026, 04, 03), new DateTime(2026, 04, 04)), cts.Token);

        // Act
        var result = await service.GetAllAsync(ct: cts.Token);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.CurrentPageNumber.Should().Be(1);
        result.CurrentPageItemsCount.Should().Be(2);
    }

    [Fact]
    public async Task GetById_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var createdEvent =
            await service.CreateAsync(CreateEventDto("Music Fest", new DateTime(2026, 05, 01), new DateTime(2026, 05, 02)), cts.Token);

        // Act
        var result = await service.GetByIdAsync(createdEvent.Id, cts.Token);

        // Assert
        result.Id.Should().Be(createdEvent.Id);
        result.Title.Should().Be("Music Fest");
    }

    [Fact]
    public async Task Update_ShouldUpdateExistingEvent_WhenEventExists()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var createdEvent =
            await service.CreateAsync(CreateEventDto("Old Title", new DateTime(2026, 06, 01), new DateTime(2026, 06, 02)), cts.Token);
        var updatedDto = CreateEventDto("New Title", new DateTime(2026, 06, 03), new DateTime(2026, 06, 04),
            "Updated description");

        // Act
        var updatedEvent = await service.UpdateAsync(createdEvent.Id, updatedDto, cts.Token);

        // Assert
        updatedEvent.Id.Should().Be(createdEvent.Id);
        updatedEvent.Title.Should().Be(updatedDto.Title);
        updatedEvent.Description.Should().Be(updatedDto.Description);
        updatedEvent.StartAt.Should().Be(updatedDto.StartAt);
        updatedEvent.EndAt.Should().Be(updatedDto.EndAt);
    }

    [Fact]
    public async Task Delete_ShouldRemoveExistingEvent_WhenEventExists()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var createdEvent =
            await service.CreateAsync(CreateEventDto("Delete Me", new DateTime(2026, 07, 01), new DateTime(2026, 07, 02)), cts.Token);

        // Act
        await service.DeleteAsync(createdEvent.Id, cts.Token);

        // Assert
        var action = () => service.GetByIdAsync(createdEvent.Id, cts.Token);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAll_ShouldFilterByTitle_WhenTitleProvided()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(
            CreateEventDto("DotNet Conference", new DateTime(2026, 03, 01), new DateTime(2026, 03, 02)), cts.Token);
        await service.CreateAsync(CreateEventDto("Java Summit", new DateTime(2026, 03, 03), new DateTime(2026, 03, 04)), cts.Token);

        // Act
        var result = await service.GetAllAsync(title: "dotnet", ct: cts.Token);

        // Assert
        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("DotNet Conference");
    }

    [Fact]
    public async Task GetAll_ShouldFilterByDateRange_WhenStartAndEndDatesProvided()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Event A", new DateTime(2026, 01, 01), new DateTime(2026, 01, 02)), cts.Token);
        await service.CreateAsync(CreateEventDto("Event B", new DateTime(2026, 01, 10), new DateTime(2026, 01, 11)), cts.Token);
        await service.CreateAsync(CreateEventDto("Event C", new DateTime(2026, 01, 20), new DateTime(2026, 01, 21)), cts.Token);

        // Act
        var result = await service.GetAllAsync(from: new DateTime(2026, 01, 05), to: new DateTime(2026, 01, 15), ct: cts.Token);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Event B");
    }

    [Fact]
    public async Task GetAll_ShouldReturnRequestedPage_WhenPaginationApplied()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Event 1", new DateTime(2026, 01, 01), new DateTime(2026, 01, 02)), cts.Token);
        await service.CreateAsync(CreateEventDto("Event 2", new DateTime(2026, 01, 03), new DateTime(2026, 01, 04)), cts.Token);
        await service.CreateAsync(CreateEventDto("Event 3", new DateTime(2026, 01, 05), new DateTime(2026, 01, 06)), cts.Token);

        // Act
        var result = await service.GetAllAsync(page: 2, pageSize: 1, ct: cts.Token);

        // Assert
        result.TotalCount.Should().Be(3);
        result.CurrentPageNumber.Should().Be(2);
        result.CurrentPageItemsCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Event 2");
    }

    [Fact]
    public async Task GetAll_ShouldApplyCombinedFiltering_WhenAllParametersProvided()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Backend Meetup", new DateTime(2026, 08, 01), new DateTime(2026, 08, 02)), cts.Token);
        await service.CreateAsync(
            CreateEventDto("Backend Deep Dive", new DateTime(2026, 08, 03), new DateTime(2026, 08, 04)), cts.Token);
        await service.CreateAsync(CreateEventDto("Frontend Meetup", new DateTime(2026, 08, 03), new DateTime(2026, 08, 04)), cts.Token);

        // Act
        var result = await service.GetAllAsync(
            title: "backend",
            from: new DateTime(2026, 08, 02),
            to: new DateTime(2026, 08, 04),
            page: 1,
            pageSize: 10,
            ct: cts.Token);

        // Assert
        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Backend Deep Dive");
    }

    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var missingId = Guid.NewGuid();

        // Act
        var action = () => service.GetByIdAsync(missingId, cts.Token);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var missingId = Guid.NewGuid();
        var dto = CreateEventDto("Updated Event", new DateTime(2026, 09, 01), new DateTime(2026, 09, 02));

        // Act
        var action = () => service.UpdateAsync(missingId, dto, cts.Token);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAll_ShouldNotApplyTitleFilter_WhenTitleIsEmptyString()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Alpha", new DateTime(2026, 12, 01), new DateTime(2026, 12, 02)), cts.Token);
        await service.CreateAsync(CreateEventDto("Beta", new DateTime(2026, 12, 03), new DateTime(2026, 12, 04)), cts.Token);

        // Act
        var result = await service.GetAllAsync(title: string.Empty, ct: cts.Token);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ShouldNotApplyTitleFilter_WhenTitleContainsOnlyWhitespace()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Alpha", new DateTime(2026, 12, 05), new DateTime(2026, 12, 06)), cts.Token);
        await service.CreateAsync(CreateEventDto("Beta", new DateTime(2026, 12, 07), new DateTime(2026, 12, 08)), cts.Token);

        // Act
        var result = await service.GetAllAsync(title: "   ", ct: cts.Token);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ShouldIncludeEvent_WhenStartAtEqualsFromBoundary()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var boundaryStart = new DateTime(2027, 01, 10, 9, 0, 0);
        await service.CreateAsync(CreateEventDto("Boundary Start", boundaryStart, new DateTime(2027, 01, 10, 12, 0, 0)), cts.Token);
        await service.CreateAsync(CreateEventDto("Earlier Event", new DateTime(2027, 01, 09, 9, 0, 0),
            new DateTime(2027, 01, 09, 12, 0, 0)), cts.Token);

        // Act
        var result = await service.GetAllAsync(from: boundaryStart, ct: cts.Token);

        // Assert
        result.Items.Should().ContainSingle(item => item.Title == "Boundary Start");
    }

    [Fact]
    public async Task GetAll_ShouldIncludeEvent_WhenEndAtEqualsToBoundary()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        var boundaryEnd = new DateTime(2027, 01, 15, 18, 0, 0);
        await service.CreateAsync(CreateEventDto("Boundary End", new DateTime(2027, 01, 15, 9, 0, 0), boundaryEnd), cts.Token);
        await service.CreateAsync(CreateEventDto("Later Event", new DateTime(2027, 01, 16, 9, 0, 0),
            new DateTime(2027, 01, 16, 18, 0, 0)), cts.Token);

        // Act
        var result = await service.GetAllAsync(to: boundaryEnd, ct: cts.Token);

        // Assert
        result.Items.Should().ContainSingle(item => item.Title == "Boundary End");
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyItems_WhenPageExceedsAvailableData()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Only Event", new DateTime(2027, 02, 01), new DateTime(2027, 02, 02)), cts.Token);

        // Act
        var result = await service.GetAllAsync(page: 3, pageSize: 1, ct: cts.Token);

        // Assert
        result.TotalCount.Should().Be(1);
        result.CurrentPageNumber.Should().Be(3);
        result.CurrentPageItemsCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllItems_WhenPageSizeEqualsTotalCount()
    {
        // Arrange
        var service = new EventService();
        var cts = new CancellationTokenSource();
        await service.CreateAsync(CreateEventDto("Event A", new DateTime(2027, 03, 01), new DateTime(2027, 03, 02)), cts.Token);
        await service.CreateAsync(CreateEventDto("Event B", new DateTime(2027, 03, 03), new DateTime(2027, 03, 04)), cts.Token);

        // Act
        var result = await service.GetAllAsync(page: 1, pageSize: 2, ct: cts.Token);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.CurrentPageItemsCount.Should().Be(2);
    }

    [Fact]
    public void EventDto_ShouldFailValidation_WhenEndAtIsEarlierThanStartAt()
    {
        // Arrange
        var dto = CreateEventDto("Invalid Dates", new DateTime(2026, 10, 10), new DateTime(2026, 10, 09));
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should()
            .Contain(result => result.ErrorMessage == "Дата завершения должна быть позже даты начала.");
    }
    
    [Fact]
    public void EventDto_ShouldFailValidation_WhenStartAtEqualsEndAt()
    {
        // Arrange
        var pointInTime = new DateTime(2027, 04, 01, 10, 0, 0);
        var dto = CreateEventDto("Equal Dates", pointInTime, pointInTime);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should()
            .Contain(result => result.ErrorMessage == "Дата завершения должна быть позже даты начала.");
    }

    private static EventDto CreateEventDto(string title, DateTime startAt, DateTime endAt,
        string? description = "Description") =>
        new(title, description, startAt, endAt);
}