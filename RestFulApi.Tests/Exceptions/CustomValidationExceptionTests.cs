using RestFulApi.Exceptions;
using Xunit;

namespace RestFulApi.Tests.Exceptions;

public class CustomValidationExceptionTests
{
    [Fact]
    public void Constructor_WithErrorsDictionary_ShouldSetErrorsAndMessage()
    {
        var errors = new Dictionary<string, ICollection<string>>
        {
            ["Title"] = ["Title is required", "Title is too short"]
        };

        var exception = new CustomValidationException(errors);

        Assert.Equal("Validation failed", exception.Message);
        Assert.Same(errors, exception.Errors);
        Assert.Equal(2, exception.Errors["Title"].Count);
    }

    [Fact]
    public void Constructor_WithFieldAndError_ShouldCreateErrorsDictionary()
    {
        const string field = "StartAt";
        const string error = "StartAt cannot be null";

        var exception = new CustomValidationException(field, error);

        Assert.Equal("Validation failed", exception.Message);
        Assert.Single(exception.Errors);
        Assert.True(exception.Errors.ContainsKey(field));
        Assert.Single(exception.Errors[field]);
        Assert.Contains(error, exception.Errors[field]);
    }
}
