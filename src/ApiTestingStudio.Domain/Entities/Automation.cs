namespace ApiTestingStudio.Domain.Entities;

/// <summary>A visual workflow definition. The node/edge graph is materialized in later sprints.</summary>
public sealed record WorkflowDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}

/// <summary>A test case that runs a workflow and evaluates assertions to Pass/Fail.</summary>
public sealed record TestCaseDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid WorkspaceId { get; init; }

    public Guid WorkflowId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}
