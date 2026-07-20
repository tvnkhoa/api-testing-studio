using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Profiles;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class RequestHistoryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeRequestExecutor _executor = new();
    private readonly InMemoryRequestHistoryRepository _history = new();
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Name = "WS" } };
    private readonly RequestExecutionService _execution;
    private readonly RequestHistoryService _sut;

    public RequestHistoryServiceTests()
    {
        _execution = new RequestExecutionService(
            _executor,
            _history,
            _session,
            new FixedClock(Now),
            new FakeVariableScopeSeeder(),
            new VariableResolver(),
            new InMemoryProfileRepository(),
            new AuthApplicator(new FakeSecretProtector()),
            new FakeRunRecorder());
        _sut = new RequestHistoryService(_history, _session);
    }

    [Fact]
    public async Task GetHistory_returns_entries_for_endpoint()
    {
        var endpointId = Guid.NewGuid();
        await _execution.SendAsync(endpointId, new HttpRequestModel { Url = "https://api.example.com/1" });
        await _execution.SendAsync(endpointId, new HttpRequestModel { Url = "https://api.example.com/2" });
        await _execution.SendAsync(Guid.NewGuid(), new HttpRequestModel { Url = "https://api.example.com/other" });

        var result = await _sut.GetHistoryAsync(endpointId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRequestForReplay_rebuilds_original_request()
    {
        var endpointId = Guid.NewGuid();
        var original = new HttpRequestModel
        {
            Method = HttpVerb.Post,
            Url = "https://api.example.com/orders",
            BodyKind = BodyKind.Json,
            Body = "{\"name\":\"widget\"}",
            Headers = [new HttpHeader("X-Test", "1")],
        };
        await _execution.SendAsync(endpointId, original);
        var entryId = _history.Entries[0].Id;

        var result = await _sut.GetRequestForReplayAsync(entryId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Method.Should().Be(HttpVerb.Post);
        result.Value.Url.Should().Be("https://api.example.com/orders");
        result.Value.Body.Should().Be("{\"name\":\"widget\"}");
        result.Value.Headers.Should().ContainSingle().Which.Name.Should().Be("X-Test");
    }

    [Fact]
    public async Task GetRequestForReplay_fails_for_unknown_entry()
    {
        var result = await _sut.GetRequestForReplayAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("request.history_not_found");
    }

    [Fact]
    public async Task ClearHistory_removes_all_entries_for_endpoint()
    {
        var endpointId = Guid.NewGuid();
        await _execution.SendAsync(endpointId, new HttpRequestModel { Url = "https://api.example.com/1" });

        var result = await _sut.ClearHistoryAsync(endpointId);

        result.IsSuccess.Should().BeTrue();
        (await _sut.GetHistoryAsync(endpointId)).Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistory_fails_when_no_workspace_open()
    {
        _session.Current = null;

        var result = await _sut.GetHistoryAsync(Guid.NewGuid());

        result.Error.Code.Should().Be("request.no_workspace");
    }
}
