using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;

namespace ApiTestingStudio.Domain.Tests;

public sealed class WorkspaceTests
{
    [Fact]
    public void New_workspace_gets_an_id_and_default_schema_version()
    {
        var workspace = new Workspace { Name = "Sample" };

        workspace.Id.Should().NotBe(Guid.Empty);
        workspace.SchemaVersion.Should().Be(Workspace.CurrentSchemaVersion);
    }

    [Fact]
    public void Endpoints_default_to_get()
    {
        var endpoint = new Endpoint { Name = "Ping", Path = "/ping" };

        endpoint.Method.Should().Be(HttpVerb.Get);
    }

    [Fact]
    public void Records_use_value_equality()
    {
        var id = Guid.NewGuid();
        var a = new Service { Id = id, WorkspaceId = id, Name = "Orders" };
        var b = new Service { Id = id, WorkspaceId = id, Name = "Orders" };

        a.Should().Be(b);
    }
}
