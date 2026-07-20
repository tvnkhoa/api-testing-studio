using System.IO;
using System.IO.Compression;
using ApiTestingStudio.Export.ApiStudio;
using ApiTestingStudio.Plugin.Abstractions.Storage;
using FluentAssertions;

namespace ApiTestingStudio.PluginHost.Tests;

/// <summary>
/// Exercises the <c>Export.ApiStudio</c> serializer end-to-end: pack a synthetic workspace
/// (db + attachments + manifest) into an <c>.apistudio</c> ZIP and unpack it, asserting the layout
/// and a byte-exact round-trip. Pure/offline — no database engine involved.
/// </summary>
public sealed class ApiStudioPackagingTests : IDisposable
{
    private readonly string _root;
    private readonly ApiStudioPackageSerializer _serializer = new();

    public ApiStudioPackagingTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ats-pkg-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task Save_writes_manifest_database_and_attachments_entries()
    {
        var (dbPath, attachmentsDir) = SeedWorkspace(withAttachments: true);
        var packagePath = Path.Combine(_root, "out.apistudio");

        await _serializer.SaveAsync(new WorkspacePackageRequest(dbPath, attachmentsDir, packagePath, Manifest()));

        File.Exists(packagePath).Should().BeTrue();
        using var archive = ZipFile.OpenRead(packagePath);
        archive.GetEntry("manifest.json").Should().NotBeNull();
        archive.GetEntry("database.sqlite").Should().NotBeNull();
        archive.GetEntry("attachments/logo.png").Should().NotBeNull();
    }

    [Fact]
    public async Task Round_trip_reproduces_database_and_attachments_and_manifest()
    {
        var (dbPath, attachmentsDir) = SeedWorkspace(withAttachments: true);
        var dbBytes = await File.ReadAllBytesAsync(dbPath);
        var packagePath = Path.Combine(_root, "out.apistudio");
        var manifest = Manifest();

        await _serializer.SaveAsync(new WorkspacePackageRequest(dbPath, attachmentsDir, packagePath, manifest));

        var staging = Path.Combine(_root, "staging");
        var contents = await _serializer.LoadAsync(packagePath, staging);

        contents.Manifest.WorkspaceId.Should().Be(manifest.WorkspaceId);
        contents.Manifest.WorkspaceSchemaVersion.Should().Be(manifest.WorkspaceSchemaVersion);
        contents.Manifest.Secrets.KeyFingerprint.Should().Be(manifest.Secrets.KeyFingerprint);
        contents.Manifest.Plugins.Should().ContainSingle(p => p.PluginId == "Runner.Stress");

        (await File.ReadAllBytesAsync(contents.DatabasePath)).Should().Equal(dbBytes);

        contents.AttachmentsDirectory.Should().NotBeNull();
        var restored = Path.Combine(contents.AttachmentsDirectory!, "logo.png");
        File.ReadAllText(restored).Should().Be("PNGDATA");
    }

    [Fact]
    public async Task Round_trip_without_attachments_reports_no_attachments_directory()
    {
        var (dbPath, _) = SeedWorkspace(withAttachments: false);
        var packagePath = Path.Combine(_root, "noattach.apistudio");

        await _serializer.SaveAsync(new WorkspacePackageRequest(dbPath, null, packagePath, Manifest()));
        var contents = await _serializer.LoadAsync(packagePath, Path.Combine(_root, "staging2"));

        contents.AttachmentsDirectory.Should().BeNull();
    }

    [Fact]
    public async Task Load_throws_when_manifest_is_missing()
    {
        var bogus = Path.Combine(_root, "bogus.apistudio");
        using (var archive = ZipFile.Open(bogus, ZipArchiveMode.Create))
        {
            archive.CreateEntry("database.sqlite");
        }

        var act = () => _serializer.LoadAsync(bogus, Path.Combine(_root, "staging3"));
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    private (string DbPath, string AttachmentsDir) SeedWorkspace(bool withAttachments)
    {
        var dbPath = Path.Combine(_root, "workspace.sqlite");
        File.WriteAllBytes(dbPath, [0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x00, 0x01, 0x02, 0x03]);

        var attachmentsDir = Path.Combine(_root, "workspace.attachments");
        if (withAttachments)
        {
            Directory.CreateDirectory(attachmentsDir);
            File.WriteAllText(Path.Combine(attachmentsDir, "logo.png"), "PNGDATA");
        }

        return (dbPath, attachmentsDir);
    }

    private static PackageManifest Manifest() => new(
        PackageManifest.CurrentFormatVersion,
        WorkspaceSchemaVersion: 9,
        AppVersion: "1.0.0",
        WorkspaceId: Guid.NewGuid(),
        WorkspaceName: "Demo",
        CreatedUtc: DateTimeOffset.UnixEpoch,
        Plugins: [new PackagePluginDependency("Runner.Stress", "Stress Runner", "1.0.0")],
        Secrets: new SecretBinding(MachineBound: true, KeyFingerprint: "abc123"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort
        }
    }
}
