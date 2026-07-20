using System.IO;
using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Packaging;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using ApiTestingStudio.Shared.Results;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

/// <summary>
/// Orchestration tests for <see cref="WorkspacePackageService"/> using in-memory fakes for the
/// serializer, DB maintenance, and workspace ports — covers manifest assembly, schema-mismatch
/// rejection on import, secret re-prompt detection, and missing-plugin reporting. See ADR-0012.
/// </summary>
public sealed class WorkspacePackageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeWorkspaceSession _session = new();
    private readonly FakePackageSerializer _serializer = new();
    private readonly FakeMaintenance _maintenance = new();
    private readonly FakePackageMetadataRepository _packages = new();
    private readonly FakeKeyStore _keyStore = new("local-fingerprint");
    private readonly FakeInstalledPlugins _plugins = new();
    private readonly RecordingWorkspaceService _workspaceService = new();

    public WorkspacePackageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ats-pkgsvc", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private WorkspacePackageService CreateService() => new(
        _session,
        _workspaceService,
        _maintenance,
        _packages,
        _keyStore,
        _plugins,
        new FixedClock(DateTimeOffset.UnixEpoch),
        [_serializer]);

    [Fact]
    public async Task Export_fails_when_no_workspace_is_open()
    {
        var result = await CreateService().ExportAsync(Path.Combine(_tempDir, "out.apistudio"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.none_open");
    }

    [Fact]
    public async Task Export_builds_manifest_from_open_workspace_and_writes_package()
    {
        var workspace = OpenWorkspace(schemaVersion: 9);
        _packages.Add(new PackageMetadata
        {
            WorkspaceId = workspace.Id,
            PluginId = "Runner.Stress",
            PluginName = "Stress Runner",
            Version = "1.0.0",
        });
        var target = Path.Combine(_tempDir, "out.apistudio");

        var result = await CreateService().ExportAsync(target);

        result.IsSuccess.Should().BeTrue();
        result.Value.PackagePath.Should().Be(target);
        _serializer.LastSaveRequest.Should().NotBeNull();

        var manifest = _serializer.LastSaveRequest!.Manifest;
        manifest.WorkspaceId.Should().Be(workspace.Id);
        manifest.WorkspaceSchemaVersion.Should().Be(9);
        manifest.Secrets.MachineBound.Should().BeTrue();
        manifest.Secrets.KeyFingerprint.Should().Be("local-fingerprint");
        manifest.Plugins.Should().ContainSingle(p => p.PluginId == "Runner.Stress");
    }

    [Fact]
    public async Task Import_rejects_a_package_written_by_a_newer_schema()
    {
        _serializer.ContentsToReturn = contents => contents with
        {
            Manifest = Manifest(_session.Current?.Id ?? Guid.NewGuid()) with
            {
                WorkspaceSchemaVersion = Workspace.CurrentSchemaVersion + 1,
            },
        };

        var result = await CreateService().ImportAsync(SeedPackageFile(), Path.Combine(_tempDir, "ws.atsdb"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.schema_too_new");
        _workspaceService.OpenedLocation.Should().BeNull();
    }

    [Fact]
    public async Task Import_flags_secrets_for_reprompt_when_key_fingerprint_differs()
    {
        var packagedWorkspaceId = Guid.NewGuid();
        _serializer.ContentsToReturn = contents => contents with
        {
            Manifest = Manifest(packagedWorkspaceId) with
            {
                Secrets = new SecretBinding(MachineBound: true, KeyFingerprint: "other-machine"),
                Plugins = [new PackagePluginDependency("Import.Curl", "cURL", "1.0.0")],
            },
        };
        _plugins.Ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // nothing installed
        _workspaceService.WorkspaceToReturn = new Workspace { Id = packagedWorkspaceId, Name = "Imported" };
        var target = Path.Combine(_tempDir, "imported.atsdb");

        var result = await CreateService().ImportAsync(SeedPackageFile(), target);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkspaceId.Should().Be(packagedWorkspaceId);
        result.Value.SecretsNeedReprompt.Should().BeTrue();
        result.Value.MissingPlugins.Should().Contain("Import.Curl");
        _workspaceService.OpenedLocation.Should().Be(target);
        File.Exists(target).Should().BeTrue();
    }

    [Fact]
    public async Task Import_does_not_flag_secrets_when_fingerprint_matches()
    {
        var id = Guid.NewGuid();
        _serializer.ContentsToReturn = contents => contents with
        {
            Manifest = Manifest(id) with { Secrets = new SecretBinding(true, "local-fingerprint") },
        };
        _workspaceService.WorkspaceToReturn = new Workspace { Id = id, Name = "Imported" };

        var result = await CreateService().ImportAsync(SeedPackageFile(), Path.Combine(_tempDir, "ok.atsdb"));

        result.IsSuccess.Should().BeTrue();
        result.Value.SecretsNeedReprompt.Should().BeFalse();
    }

    private Workspace OpenWorkspace(int schemaVersion)
    {
        var workspace = new Workspace { Name = "Demo", SchemaVersion = schemaVersion };
        _session.Current = workspace;
        _session.Location = Path.Combine(_tempDir, "demo.atsdb");
        return workspace;
    }

    private string SeedPackageFile()
    {
        var path = Path.Combine(_tempDir, "in.apistudio");
        File.WriteAllText(path, "dummy-package");
        return path;
    }

    private static PackageManifest Manifest(Guid workspaceId) => new(
        PackageManifest.CurrentFormatVersion,
        Workspace.CurrentSchemaVersion,
        "1.0.0",
        workspaceId,
        "Imported",
        DateTimeOffset.UnixEpoch,
        [],
        new SecretBinding(true, "local-fingerprint"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort
        }
    }

    // ---- fakes ----

    private sealed class FakePackageSerializer : IWorkspaceSerializer
    {
        public string Format => "apistudio";

        public WorkspacePackageRequest? LastSaveRequest { get; private set; }

        public Func<WorkspacePackageContents, WorkspacePackageContents> ContentsToReturn { get; set; } = c => c;

        public Task SaveAsync(WorkspacePackageRequest request, CancellationToken cancellationToken = default)
        {
            LastSaveRequest = request;
            File.WriteAllText(request.TargetPackagePath, "packaged");
            return Task.CompletedTask;
        }

        public Task<WorkspacePackageContents> LoadAsync(string packagePath, string stagingDirectory, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(stagingDirectory);
            var dbPath = Path.Combine(stagingDirectory, "database.sqlite");
            File.WriteAllText(dbPath, "restored-db");

            var contents = new WorkspacePackageContents(
                new PackageManifest(PackageManifest.CurrentFormatVersion, Workspace.CurrentSchemaVersion, "1.0.0",
                    Guid.NewGuid(), "Imported", DateTimeOffset.UnixEpoch, [], new SecretBinding(true, "local-fingerprint")),
                dbPath,
                AttachmentsDirectory: null);

            return Task.FromResult(ContentsToReturn(contents));
        }

        public Task<PackageManifest> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeMaintenance : IWorkspaceMaintenance
    {
        public Task CheckpointAndVacuumAsync(string sourceDatabasePath, string targetDatabasePath, CancellationToken cancellationToken = default)
        {
            File.WriteAllText(targetDatabasePath, "vacuumed");
            return Task.CompletedTask;
        }
    }

    private sealed class FakePackageMetadataRepository : IPackageMetadataRepository
    {
        private readonly List<PackageMetadata> _items = [];

        public void Add(PackageMetadata item) => _items.Add(item);

        public Task<IReadOnlyList<PackageMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PackageMetadata>>(_items);

        public Task UpsertAsync(PackageMetadata package, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAsync(string pluginId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeKeyStore : IKeyStore
    {
        private readonly string _fingerprint;

        public FakeKeyStore(string fingerprint) => _fingerprint = fingerprint;

        public byte[] GetOrCreateMasterKey() => new byte[32];

        public string GetKeyFingerprint() => _fingerprint;
    }

    private sealed class FakeInstalledPlugins : IInstalledPluginCatalog
    {
        public HashSet<string> Ids { get; set; } = new(StringComparer.OrdinalIgnoreCase) { "Runner.Stress" };

        public IReadOnlyCollection<string> InstalledPluginIds => Ids;
    }

    private sealed class RecordingWorkspaceService : IWorkspaceService
    {
        public string? OpenedLocation { get; private set; }

        public Workspace WorkspaceToReturn { get; set; } = new() { Id = Guid.NewGuid(), Name = "Imported" };

        public Task<Result<Workspace>> OpenAsync(string location, CancellationToken cancellationToken = default)
        {
            OpenedLocation = location;
            return Task.FromResult(Result.Success(WorkspaceToReturn));
        }

        public Task<Result> CloseAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());

        public Task<Result<Workspace>> CreateAsync(string location, string name, string? description = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> DeleteAsync(string location, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
