using ApiTestingStudio.Application.Workspaces;
using ApiTestingStudio.Domain.Entities;
using FluentAssertions;

namespace ApiTestingStudio.Application.Tests;

public sealed class SchemaVersionValidatorTests
{
    [Fact]
    public void Current_version_is_valid()
    {
        SchemaVersionValidator.Validate(Workspace.CurrentSchemaVersion).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Older_version_is_valid()
    {
        SchemaVersionValidator.Validate(Workspace.CurrentSchemaVersion - 1).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Newer_version_is_rejected_as_schema_too_new()
    {
        var result = SchemaVersionValidator.Validate(Workspace.CurrentSchemaVersion + 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workspace.schema_too_new");
    }
}
