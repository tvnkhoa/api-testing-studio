using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Packaging;

/// <summary>Typed, transport-agnostic errors for packaging (export/import) operations.</summary>
public static class PackageErrors
{
    public static Error NoSerializer { get; } =
        new("package.no_serializer", "No exporter is available to read or write this package format.");

    public static Error Unreadable(string path) =>
        new("package.unreadable", $"The package at '{path}' could not be read (missing or corrupt).");

    public static Error FormatTooNew(string fileVersion, string appVersion) =>
        new(
            "package.format_too_new",
            FormattableString.Invariant(
                $"The package format version {fileVersion} is newer than this build supports ({appVersion})."));
}
