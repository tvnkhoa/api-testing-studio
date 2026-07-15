using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using ApiTestingStudio.Plugin.Abstractions.Importing;
using ApiTestingStudio.Shared.Results;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class ImportOrchestratorTests
{
    private readonly FakeWorkspaceSession _session = new() { Current = new Workspace { Name = "W" } };
    private readonly FakeDefinitionFetcher _fetcher = new();
    private readonly FakeCatalogMerger _merger = new();

    [Fact]
    public async Task PreviewAsync_fails_when_no_workspace_open()
    {
        _session.Current = null;
        var orchestrator = Build([new FakeImporter("openapi")]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Content = "{\"openapi\":\"3.0.0\"}" });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("import.no_workspace");
    }

    [Fact]
    public async Task PreviewAsync_fails_when_no_input()
    {
        var orchestrator = Build([new FakeImporter("openapi")]);

        var result = await orchestrator.PreviewAsync(new ImportRequest());

        result.Error.Code.Should().Be("import.nothing_to_import");
    }

    [Fact]
    public async Task PreviewAsync_fails_when_no_importer_matches()
    {
        var importer = new FakeImporter("openapi") { CanImportResult = false };
        var orchestrator = Build([importer]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Content = "mystery content" });

        result.Error.Code.Should().Be("import.unknown_format");
    }

    [Fact]
    public async Task PreviewAsync_wraps_parser_exceptions()
    {
        var importer = new FakeImporter("curl", toThrow: new InvalidOperationException("bad curl"));
        var orchestrator = Build([importer]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Format = "curl", Content = "curl x" });

        result.Error.Code.Should().Be("import.parse_failed");
        result.Error.Message.Should().Contain("bad curl");
    }

    [Fact]
    public async Task PreviewAsync_fails_when_result_empty()
    {
        var importer = new FakeImporter("curl", ImportResult.Empty);
        var orchestrator = Build([importer]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Format = "curl", Content = "curl x" });

        result.Error.Code.Should().Be("import.nothing_found");
    }

    [Fact]
    public async Task PreviewAsync_builds_create_preview_against_empty_catalog()
    {
        var importer = new FakeImporter("openapi", TwoEndpointResult(out _));
        var orchestrator = Build([importer]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Format = "openapi", Content = "{\"openapi\":\"3.0.0\"}" });

        result.IsSuccess.Should().BeTrue();
        result.Value.EndpointCount.Should().Be(2);
        var service = result.Value.Services.Should().ContainSingle().Subject;
        service.Change.Should().Be(ImportChangeKind.Create);
        service.Endpoints.Should().OnlyContain(e => e.Change == ImportChangeKind.Create);
    }

    [Fact]
    public async Task PreviewAsync_marks_matching_endpoints_as_updates()
    {
        var importResult = TwoEndpointResult(out var service);
        var existing = new ServiceCatalogTree(
        [
            new ServiceNode(
                Guid.NewGuid(), service.Name, service.BaseUrl, null, 0,
                [],
                [new EndpointNode(Guid.NewGuid(), Guid.NewGuid(), null, "List", HttpVerb.Get, "/pets", null, 0)]),
        ]);

        var orchestrator = Build([new FakeImporter("openapi", importResult)], new FakeCatalogReader(existing));

        var result = await orchestrator.PreviewAsync(new ImportRequest { Format = "openapi", Content = "x" });

        var previewed = result.Value.Services.Single();
        previewed.Change.Should().Be(ImportChangeKind.Update);
        previewed.Endpoints.Single(e => e.Path == "/pets").Change.Should().Be(ImportChangeKind.Update);
        previewed.Endpoints.Single(e => e.Path == "/pets/{id}").Change.Should().Be(ImportChangeKind.Create);
    }

    [Fact]
    public async Task PreviewAsync_fetches_url_when_content_absent()
    {
        _fetcher.Next = Result.Success(new FetchedDefinition("{\"openapi\":\"3.0.0\"}", "https://host/openapi.json", Format: null));
        var orchestrator = Build([new FakeImporter("openapi", TwoEndpointResult(out _))]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Uri = "https://host" });

        result.IsSuccess.Should().BeTrue();
        _fetcher.LastUri.Should().Be("https://host");
    }

    [Fact]
    public async Task PreviewAsync_propagates_fetch_failure()
    {
        _fetcher.Next = Result.Failure<FetchedDefinition>(ImportErrors.FetchFailed("boom"));
        var orchestrator = Build([new FakeImporter("openapi")]);

        var result = await orchestrator.PreviewAsync(new ImportRequest { Uri = "https://host" });

        result.Error.Code.Should().Be("import.fetch_failed");
    }

    [Fact]
    public async Task CommitAsync_delegates_to_merger_with_options()
    {
        var orchestrator = Build([new FakeImporter("openapi", TwoEndpointResult(out _))]);
        var preview = (await orchestrator.PreviewAsync(new ImportRequest { Format = "openapi", Content = "x" })).Value;

        var summary = await orchestrator.CommitAsync(preview, new ImportOptions { OverwriteExisting = true });

        summary.IsSuccess.Should().BeTrue();
        _merger.MergeCount.Should().Be(1);
        _merger.LastOptions!.OverwriteExisting.Should().BeTrue();
        _merger.LastResult.Should().BeSameAs(preview.Result);
    }

    private ImportOrchestrator Build(IEnumerable<IImporter> importers, IServiceExplorerService? catalog = null) =>
        new(importers, new SourceFormatDetector(), _fetcher, _merger, catalog ?? new FakeCatalogReader(), _session);

    private static ImportResult TwoEndpointResult(out Service service)
    {
        service = new Service { Name = "Petstore", BaseUrl = "https://api.pet.com" };
        var endpoints = new List<Endpoint>
        {
            new() { ServiceId = service.Id, Name = "List", Method = HttpVerb.Get, Path = "/pets" },
            new() { ServiceId = service.Id, Name = "Get", Method = HttpVerb.Get, Path = "/pets/{id}" },
        };
        return new ImportResult([service], endpoints);
    }
}
