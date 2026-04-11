using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RestFulApi.DTOs;
using RestFulApi.Interfaces;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests;

public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public void Program_ShouldRegisterApplicationServices_AsSingleton()
    {
        // Act
        var eventServiceFirst = _factory.Services.GetRequiredService<IEventService>();
        var eventServiceSecond = _factory.Services.GetRequiredService<IEventService>();

        var bookingServiceFirst = _factory.Services.GetRequiredService<IBookingService>();
        var bookingServiceSecond = _factory.Services.GetRequiredService<IBookingService>();

        var queueFirst = _factory.Services.GetRequiredService<IBookingTaskQueue>();
        var queueSecond = _factory.Services.GetRequiredService<IBookingTaskQueue>();

        // Assert
        eventServiceFirst.Should().BeSameAs(eventServiceSecond);
        bookingServiceFirst.Should().BeSameAs(bookingServiceSecond);
        queueFirst.Should().BeSameAs(queueSecond);

        _factory.Services.GetServices<IHostedService>()
            .Should().ContainSingle(service => service is BookingBackgroundService);
    }

    [Fact]
    public void Program_ShouldConfigureJsonEnumConverter()
    {
        // Act
        var jsonOptions = _factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value;

        // Assert
        jsonOptions.JsonSerializerOptions.Converters
            .Should().Contain(converter => converter is JsonStringEnumConverter);
    }

    [Fact]
    public async Task Program_ShouldUseGlobalExceptionHandlingMiddleware_AndReturnProblemDetails()
    {
        // Arrange
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        // Act
        var response = await client.GetAsync($"/Events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(responseJson);

        document.RootElement.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.NotFound);
        document.RootElement.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Program_ShouldSerializeBookingStatus_AsStringInApiResponses()
    {
        // Arrange
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var now = DateTime.UtcNow;
        var eventDto = new EventDto(
            "Program test event",
            "Event for Program startup tests",
            now.AddDays(1),
            now.AddDays(2),
            10);

        var createEventResponse = await client.PostAsJsonAsync("/Events", eventDto, TestContext.Current.CancellationToken);
        createEventResponse.EnsureSuccessStatusCode();

        var createdEventJson = await createEventResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createdEventDocument = JsonDocument.Parse(createdEventJson);
        var eventId = createdEventDocument.RootElement.GetProperty("id").GetGuid();

        // Act
        var bookingResponse = await client.PostAsync($"/Events/{eventId}/book", null, TestContext.Current.CancellationToken);

        // Assert
        bookingResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var bookingJson = await bookingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var bookingDocument = JsonDocument.Parse(bookingJson);

        var statusProperty = bookingDocument.RootElement.GetProperty("status");
        statusProperty.ValueKind.Should().Be(JsonValueKind.String);
        statusProperty.GetString().Should().Be("Pending");
    }
}
