using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Shared.Results;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class RequestExecutionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeRequestExecutor _executor = new();
    private readonly InMemoryRequestHistoryRepository _history = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Name = "WS" } };
    private readonly RequestExecutionService _sut;

    public RequestExecutionServiceTests()
    {
        _sut = new RequestExecutionService(
            _executor,
            _history,
            _session,
            new FixedClock(Now),
            new FakeVariableScopeSeeder(),
            new VariableResolver(),
            new InMemoryProfileRepository(),
            new AuthApplicator(new FakeSecretProtector()));
    }

    [Fact]
    public async Task SendAsync_records_history_on_success()
    {
        var endpointId = Guid.NewGuid();
        var request = new HttpRequestModel { Method = HttpVerb.Get, Url = "https://api.example.com/orders" };

        var result = await _sut.SendAsync(endpointId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Response.StatusCode.Should().Be(200);
        _history.Entries.Should().ContainSingle();
        var entry = _history.Entries[0];
        entry.EndpointId.Should().Be(endpointId);
        entry.Method.Should().Be(HttpVerb.Get);
        entry.Url.Should().Be("https://api.example.com/orders");
        entry.StatusCode.Should().Be(200);
        entry.TimestampUtc.Should().Be(Now);
        entry.RequestSnapshot.Should().NotBeNullOrEmpty();
        entry.ResponseSnapshot.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAsync_fails_when_no_workspace_open()
    {
        _session.Current = null;
        var request = new HttpRequestModel { Url = "https://api.example.com" };

        var result = await _sut.SendAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("request.no_workspace");
        _executor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_fails_when_url_blank()
    {
        var request = new HttpRequestModel { Url = "   " };

        var result = await _sut.SendAsync(Guid.NewGuid(), request);

        result.Error.Code.Should().Be("request.url_required");
        _executor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_fails_for_non_absolute_url()
    {
        var request = new HttpRequestModel { Url = "/orders" };

        var result = await _sut.SendAsync(Guid.NewGuid(), request);

        result.Error.Code.Should().Be("request.invalid_url");
        _executor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_does_not_record_history_when_execution_fails()
    {
        _executor.ResultToReturn = Result.Failure<HttpExecutionResult>(RequestExecutionErrors.Cancelled);
        var request = new HttpRequestModel { Url = "https://api.example.com" };

        var result = await _sut.SendAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("request.cancelled");
        _history.Entries.Should().BeEmpty();
    }
}
