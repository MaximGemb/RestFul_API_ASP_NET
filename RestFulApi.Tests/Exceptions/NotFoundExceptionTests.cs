using RestFulApi.Exceptions;
using Xunit;

namespace RestFulApi.Tests.Exceptions;

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithIdMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var message = "Test error message";
        var innerException = new Exception("Inner error");

        // Act
        var exception = new NotFoundException(id, message, innerException);

        // Assert
        Assert.Equal(id, exception.Id);
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultMessage()
    {
        // Act
        var exception = new NotFoundException();

        // Assert
        Assert.Null(exception.Id);
        Assert.Equal("Unknown event error.", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithIdAndMessage_ShouldSetIdAndMessage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var message = "Custom error message";

        // Act
        var exception = new NotFoundException(id, message);

        // Assert
        Assert.Equal(id, exception.Id);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
}
