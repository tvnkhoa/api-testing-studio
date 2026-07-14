namespace ApiTestingStudio.Shared.Results;

/// <summary>
/// A structured, transport-agnostic error. Prefer returning <see cref="Result"/> /
/// <see cref="Result{T}"/> over throwing for expected, recoverable failures.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>Sentinel value representing "no error".</summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
