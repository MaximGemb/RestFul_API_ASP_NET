using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestFulApi.DataAccess;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RestFulApi.DTOs;
using RestFulApi.Interfaces;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests;

public class ProgramTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public void Program_ShouldRegisterApplicationServices_AsScoped()
    {
        // Act
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();

        var eventServiceScope1First = scope1.ServiceProvider.GetRequiredService<IEventService>();
        var eventServiceScope1Second = scope1.ServiceProvider.GetRequiredService<IEventService>();
        var eventServiceScope2 = scope2.ServiceProvider.GetRequiredService<IEventService>();

        var bookingServiceScope1First = scope1.ServiceProvider.GetRequiredService<IBookingService>();
        var bookingServiceScope1Second = scope1.ServiceProvider.GetRequiredService<IBookingService>();
        var bookingServiceScope2 = scope2.ServiceProvider.GetRequiredService<IBookingService>();

        // Assert
        eventServiceScope1First.Should().BeSameAs(eventServiceScope1Second);
        bookingServiceScope1First.Should().BeSameAs(bookingServiceScope1Second);

        eventServiceScope1First.Should().NotBeSameAs(eventServiceScope2);
        bookingServiceScope1First.Should().NotBeSameAs(bookingServiceScope2);

        factory.Services.GetServices<IHostedService>()
            .Should().ContainSingle(service => service is BookingBackgroundService);
    }

    [Fact]
    public void Program_ShouldConfigureJsonEnumConverter()
    {
        // Act
        var jsonOptions = factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value;

        // Assert
        jsonOptions.JsonSerializerOptions.Converters
            .Should().Contain(converter => converter is JsonStringEnumConverter);
    }

    [Fact]
    public async Task Program_ShouldUseGlobalExceptionHandlingMiddleware_AndReturnProblemDetails()
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
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
    public void Program_ShouldRegisterAppDbContext_AsScoped()
    {
        // Act
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();

        var db1a = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var db1b = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        // Assert
        db1a.Should().BeSameAs(db1b);
        db1a.Should().NotBeSameAs(db2);
    }

    [Fact]
    public async Task Program_ShouldSerializeBookingStatus_AsStringInApiResponses()
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var now = DateTime.UtcNow;
        var eventDto = new CreateEvent
        {
            Title = "Program test event",
            Description = "Event for Program startup tests",
            StartAt = now.AddDays(1),
            EndAt = now.AddDays(2),
            TotalSeats = 10
        };

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

public class NpgsqlDbContextConfigurationTests(NpgsqlProviderVerificationFactory factory)
    : IClassFixture<NpgsqlProviderVerificationFactory>
{
    [Fact]
    public void Program_ShouldConfigureDbContext_WithNpgsqlProvider()
    {
        _ = factory.Server;

        factory.CapturedExtensions
            .Should().Contain(e => e.GetType().FullName!
                .Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Program_ShouldConfigureDbContext_WithConnectionStringFromConfiguration()
    {
        _ = factory.Server;

        var configConnStr = factory.Services
            .GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection");

        configConnStr.Should().NotBeNullOrWhiteSpace("DefaultConnection must be present in configuration");
        factory.CapturedConnectionString.Should().NotBeNullOrWhiteSpace();

        var configParts = configConnStr!.Split(';')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (configParts.TryGetValue("Host", out var host))
            factory.CapturedConnectionString.Should().Contain(host);
        if (configParts.TryGetValue("Database", out var db))
            factory.CapturedConnectionString.Should().Contain(db);
    }
}

public class NpgsqlProviderVerificationFactory : WebApplicationFactory<Program>
{
    public IDbContextOptionsExtension[] CapturedExtensions { get; private set; } = [];
    public string? CapturedConnectionString { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
#pragma warning disable ASP0000
            using var sp = services.BuildServiceProvider();
#pragma warning restore ASP0000
            var opts = sp.GetService<DbContextOptions<AppDbContext>>();
            CapturedExtensions = opts?.Extensions.ToArray() ?? [];

            var relationalExt = CapturedExtensions.FirstOrDefault(e =>
                e.GetType().GetProperty("ConnectionString") != null);
            CapturedConnectionString = relationalExt?.GetType()
                .GetProperty("ConnectionString")?.GetValue(relationalExt) as string;

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o =>
                o.UseInMemoryDatabase("NpgsqlVerification_" + Guid.NewGuid()));
        });
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            var dbName = "ProgramTests_" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });
    }
}
