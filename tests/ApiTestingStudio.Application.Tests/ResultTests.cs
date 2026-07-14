using ApiTestingStudio.Shared.Results;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_result_carries_value_and_reports_success()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_result_reports_failure_and_error()
    {
        var error = new Error("test.code", "boom");

        var result = Result.Failure<int>(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Accessing_value_of_failure_throws()
    {
        var result = Result.Failure<int>(new Error("x", "y"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }
}
