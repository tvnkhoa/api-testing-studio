using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Testing;

/// <summary>
/// Typed errors for test-case / suite execution, returned via <see cref="Result"/>. Codes are
/// namespaced under <c>test.*</c>.
/// </summary>
public static class TestingErrors
{
    public static Error NoWorkspaceOpen { get; } =
        new("test.no_workspace", "No workspace is currently open.");

    public static Error CaseNotFound(Guid id) =>
        new("test.case_not_found", $"Test case '{id}' was not found.");

    public static Error SuiteNotFound(Guid id) =>
        new("test.suite_not_found", $"Test suite '{id}' was not found.");
}
