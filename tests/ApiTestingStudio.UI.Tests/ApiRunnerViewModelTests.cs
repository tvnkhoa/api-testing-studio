using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.UI.Messaging;
using ApiTestingStudio.UI.ViewModels.Runner;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiTestingStudio.UI.Tests;

public sealed class ApiRunnerViewModelTests
{
    private readonly FakeRequestExecutionService _execution = new();
    private readonly FakeRequestHistoryService _history = new();
    private readonly FakeEndpointRepository _endpoints = new();
    private readonly FakeServiceRepository _services = new();
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly FakeStatusBarService _status = new();

    private ApiRunnerViewModel CreateViewModel() => new(
        _execution, _history, _endpoints, _services, _messenger, _status,
        NullLogger<ApiRunnerViewModel>.Instance);

    private (Guid ServiceId, Guid EndpointId) SeedEndpoint(HttpVerb method = HttpVerb.Post, string path = "/orders")
    {
        var service = new Service { Name = "Orders", BaseUrl = "https://api.example.com" };
        var endpoint = new Endpoint { ServiceId = service.Id, Name = "Create", Method = method, Path = path };
        _services.Services.Add(service);
        _endpoints.Endpoints.Add(endpoint);
        return (service.Id, endpoint.Id);
    }

    [Fact]
    public void EndpointSelected_message_loads_builder_from_endpoint_and_service_base_url()
    {
        var vm = CreateViewModel();
        var (serviceId, endpointId) = SeedEndpoint();

        _messenger.Send(new EndpointSelectedMessage(endpointId, serviceId, "Create", HttpVerb.Post, "/orders"));

        vm.Builder.Method.Should().Be(HttpVerb.Post);
        vm.Builder.Url.Should().Be("https://api.example.com/orders");
        vm.Title.Should().Be("Create");
    }

    [Fact]
    public async Task Send_populates_the_response_viewer()
    {
        var vm = CreateViewModel();
        var (serviceId, endpointId) = SeedEndpoint(HttpVerb.Get, "/ping");
        _messenger.Send(new EndpointSelectedMessage(endpointId, serviceId, "Create", HttpVerb.Get, "/ping"));

        await vm.SendCommand.ExecuteAsync(null);

        _execution.Sent.Should().ContainSingle();
        vm.Response.HasResponse.Should().BeTrue();
        vm.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Replay_reloads_the_stored_request_and_resends()
    {
        var vm = CreateViewModel();
        var entry = new RequestHistoryEntry
        {
            EndpointId = Guid.NewGuid(),
            Url = "https://api.example.com/replay",
            RequestSnapshot = "{}",
            ResponseSnapshot = "{}",
        };
        vm.History.Add(entry);
        vm.SelectedHistoryEntry = entry;
        _history.ReplayRequest = new HttpRequestModel { Method = HttpVerb.Get, Url = "https://api.example.com/replay" };

        await vm.ReplayCommand.ExecuteAsync(null);

        vm.Builder.Url.Should().Be("https://api.example.com/replay");
        _execution.Sent.Should().ContainSingle();
    }
}
