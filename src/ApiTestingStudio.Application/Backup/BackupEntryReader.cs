using System.IO;
using ApiTestingStudio.Plugin.Abstractions.Storage;

namespace ApiTestingStudio.Application.Backup;

/// <summary>
/// Builds <see cref="BackupEntry"/> values from a package's manifest (cheap manifest-only read).
/// Shared by the backup and recovery services. A corrupt/foreign file in a backup folder resolves to
/// null so listing skips it rather than failing. See ADR-0012.
/// </summary>
public sealed class BackupEntryReader
{
    private readonly IWorkspaceSerializer? _serializer;

    public BackupEntryReader(IEnumerable<IWorkspaceSerializer> serializers)
    {
        _serializer = serializers?.FirstOrDefault(s => s.Format == "apistudio");
    }

    public async Task<BackupEntry?> TryReadAsync(string file, CancellationToken cancellationToken)
    {
        if (_serializer is null)
        {
            return null;
        }

        try
        {
            var manifest = await _serializer.ReadManifestAsync(file, cancellationToken).ConfigureAwait(false);
            return new BackupEntry(
                file,
                manifest.WorkspaceId,
                manifest.WorkspaceName,
                manifest.CreatedUtc,
                new FileInfo(file).Length);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            return null;
        }
    }
}
