using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RestFulApi.Exceptions;
using RestFulApi.Middleware;
using Xunit;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace RestFulApi.Tests.Middleware;

public class GlobalExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _loggerMock = new();
    private readonly DefaultHttpContext _httpContext = new()
    {
        Response =
        {
            Body = new MemoryStream()
        }
    };

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNoExceptionThrown()
    {
        // Arrange
        var isNextCalled = false;

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.True(isNextCalled);
        Assert.Equal(StatusCodes.Status200OK, _httpContext.Response.StatusCode);
        return;

        Task Next(HttpContext _)
        {
            isNextCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleNotFoundException_AndReturn404()
    {
        // Arrange
        const string expectedMessage = "Test not found error";
        var exception = new NotFoundException(Guid.NewGuid(), expectedMessage);

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, _httpContext.Response.StatusCode);
        Assert.StartsWith("application/json", _httpContext.Response.ContentType);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
#pragma warning disable CA1869
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
#pragma warning restore CA1869

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal(expectedMessage, problemDetails.Detail);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleValidationException_AndReturn400()
    {
        // Arrange
        const string expectedMessage = "Validation error";
        var exception = new ValidationException(expectedMessage);

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal(expectedMessage, problemDetails.Detail);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleGenericException_AndReturn500WithGenericMessage()
    {
        // Arrange
        var exception = new Exception("Sensitive internal error message");

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
        Assert.NotEqual("Sensitive internal error message", problemDetails.Detail);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleOperationCanceledException_AndReturn499WithoutBody()
    {
        // Arrange
        var exception = new OperationCanceledException("Task cancelled");

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(499, _httpContext.Response.StatusCode);
        
        // Assert no body was written
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Empty(responseBody);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotWriteResponse_WhenResponseHasAlreadyStarted()
    {
        // Arrange
        var exception = new Exception("Test exception");
        
        // Setup a mock HttpContext where Response.HasStarted is true
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var mockRequest = new Mock<HttpRequest>();
        
        mockResponse.Setup(r => r.HasStarted).Returns(true);
        // Required for logging
        mockRequest.Setup(r => r.Method).Returns("GET");
        mockRequest.Setup(r => r.Path).Returns("/api/test");
        
        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(mockHttpContext.Object);

        // Assert
        // We verify that Status code wasn't attempted to be set (which happens after the HasStarted check)
        mockResponse.VerifySet(r => r.StatusCode = It.IsAny<int>(), Times.Never);
        mockResponse.VerifySet(r => r.ContentType = It.IsAny<string>(), Times.Never);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleNoAvailableSeatsException_AndReturn409()
    {
        // Arrange
        const string expectedMessage = "No seats left";
        var exception = new NoAvailableSeatsException(Guid.NewGuid(), expectedMessage);

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal(expectedMessage, problemDetails.Detail);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogInformation_WhenOperationCanceledExceptionThrown()
    {
        // Arrange
        var exception = new OperationCanceledException("Task cancelled");
        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Request was cancelled.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogErrorWithRequestContext_WhenNonCanceledExceptionThrown()
    {
        // Arrange
        var exception = new Exception("boom");
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Path = "/api/bookings";

        var middleware = new GlobalExceptionHandlingMiddleware(Next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Unhandled exception") &&
                    v.ToString()!.Contains("POST") &&
                    v.ToString()!.Contains("/api/bookings")),
                It.Is<Exception>(e => ReferenceEquals(e, exception)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        return;

        Task Next(HttpContext _) => throw exception;
    }

    // --- MapStatusCode ---

    public static IEnumerable<object[]> ExceptionToStatusAndTitleData =>
    [
        [new ValidationException("val error"), StatusCodes.Status400BadRequest, "Validation Error"],
        [new CustomValidationException("Field", "Required"), StatusCodes.Status400BadRequest, "Validation Error"],
        [new NotFoundException(Guid.NewGuid(), "not found"), StatusCodes.Status404NotFound, "Not Found"],
        [new NoAvailableSeatsException(Guid.NewGuid(), "no seats"), StatusCodes.Status409Conflict, "No Available Seats"],
        [new Exception("generic"), StatusCodes.Status500InternalServerError, "Internal Server Error"],
    ];

    [Theory]
    [MemberData(nameof(ExceptionToStatusAndTitleData))]
    public async Task MapStatusCode_ShouldReturnCorrectStatusCodeAndTitle(Exception exception, int expectedStatus, string expectedTitle)
    {
        // Arrange
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new GlobalExceptionHandlingMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(expectedStatus, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
#pragma warning disable CA1869
        var pd = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
#pragma warning restore CA1869
        Assert.NotNull(pd);
        Assert.Equal(expectedTitle, pd.Title);
    }

    [Fact]
    public async Task MapStatusCode_ShouldReturn499_ForOperationCanceledException()
    {
        // Arrange
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(), _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(499, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Empty(body);
    }

    // --- CustomValidationException branch ---

    [Fact]
    public async Task InvokeAsync_ShouldHandleCustomValidationException_WithSingleError_AndReturnValidationProblemDetails()
    {
        // Arrange
        var exception = new CustomValidationException("Name", "Name is required");
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new GlobalExceptionHandlingMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Validation Error", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("Validation failed", doc.RootElement.GetProperty("detail").GetString());

        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out var nameErrors));
        Assert.Contains("Name is required", nameErrors.EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleCustomValidationException_WithMultipleErrors_AndReturnAllFields()
    {
        // Arrange
        var errorsDict = new Dictionary<string, ICollection<string>>
        {
            { "Title", new List<string> { "Title is required", "Title is too short" } },
            { "Date", new List<string> { "Date must be in the future" } }
        };
        var exception = new CustomValidationException(errorsDict);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new GlobalExceptionHandlingMiddleware(_ => throw exception, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);

        var errors = doc.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("Title", out var titleErrors));
        var titleErrorList = titleErrors.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Title is required", titleErrorList);
        Assert.Contains("Title is too short", titleErrorList);

        Assert.True(errors.TryGetProperty("Date", out var dateErrors));
        Assert.Contains("Date must be in the future", dateErrors.EnumerateArray().Select(e => e.GetString()));
    }
}
