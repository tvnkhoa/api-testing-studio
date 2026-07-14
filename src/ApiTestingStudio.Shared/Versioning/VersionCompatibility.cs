using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Shared.Versioning;

/// <summary>
/// Pure helper that checks whether a subject version falls within an inclusive
/// <c>[minimum, maximum]</c> range. Used to gate a host API version against a plugin's declared
/// supported range without either side depending on plugin contracts.
/// </summary>
public static class VersionCompatibility
{
    /// <summary>Error code returned when the subject version is below the minimum.</summary>
    public const string BelowMinimumCode = "version.below_minimum";

    /// <summary>Error code returned when the subject version is above the maximum.</summary>
    public const string AboveMaximumCode = "version.above_maximum";

    /// <summary>
    /// Returns success when <paramref name="version"/> is within <c>[minimum, maximum]</c>
    /// (inclusive). A <c>null</c> <paramref name="maximum"/> means no upper bound. On failure the
    /// returned <see cref="Error"/> carries a typed code and a human-readable reason.
    /// </summary>
    public static Result Check(Version version, Version minimum, Version? maximum = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(minimum);

        if (version < minimum)
        {
            return Result.Failure(new Error(
                BelowMinimumCode,
                $"Version {version} is below the required minimum {minimum}."));
        }

        if (maximum is not null && version > maximum)
        {
            return Result.Failure(new Error(
                AboveMaximumCode,
                $"Version {version} is above the supported maximum {maximum}."));
        }

        return Result.Success();
    }
}
