using System.Text.Json;
using System.Text.Json.Serialization;
using ApiTestingStudio.Domain.Entities;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Serializes the per-assertion <see cref="AssertionResult"/> array into / out of the
/// <see cref="TestRunResult.DetailsJson"/> column (<c>System.Text.Json</c>), mirroring the
/// request/response snapshot pattern used by history.
/// </summary>
public static class TestResultDetails
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(IReadOnlyList<AssertionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return JsonSerializer.Serialize(results, Options);
    }

    public static IReadOnlyList<AssertionResult> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AssertionResult>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
