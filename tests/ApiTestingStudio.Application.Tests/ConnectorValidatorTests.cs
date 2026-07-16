using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ApiTestingStudio.Application.Tests;

public sealed class ConnectorValidatorTests
{
    private readonly ConnectorValidator _validator = new();
    private readonly Guid _sourceId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    [Fact]
    public void Validate_valid_output_to_input_succeeds()
    {
        var result = _validator.Validate(Request(WorkflowPorts.Next, WorkflowPorts.In));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_self_connection_is_rejected()
    {
        var request = Request(WorkflowPorts.Next, WorkflowPorts.In) with { TargetNodeId = _sourceId };

        var result = _validator.Validate(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowErrors.ConnectSelf);
    }

    [Fact]
    public void Validate_unknown_source_port_is_rejected()
    {
        var result = _validator.Validate(Request("bogus", WorkflowPorts.In));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.connect_unknown_port");
    }

    [Fact]
    public void Validate_reversed_direction_is_rejected_as_unknown_port()
    {
        // Using an input port name on the source side is not a valid output port.
        var result = _validator.Validate(Request(WorkflowPorts.In, WorkflowPorts.In));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.connect_unknown_port");
    }

    [Fact]
    public void Validate_duplicate_edge_is_rejected()
    {
        var existing = new WorkflowEdge
        {
            SourceNodeId = _sourceId,
            TargetNodeId = _targetId,
            SourcePort = WorkflowPorts.Next,
            TargetPort = WorkflowPorts.In,
        };
        var request = Request(WorkflowPorts.Next, WorkflowPorts.In) with { ExistingEdges = [existing] };

        var result = _validator.Validate(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowErrors.ConnectDuplicate);
    }

    private ConnectionRequest Request(string sourcePort, string targetPort) => new()
    {
        SourceNodeId = _sourceId,
        SourceKind = WorkflowNodeKind.Api,
        SourcePort = sourcePort,
        TargetNodeId = _targetId,
        TargetKind = WorkflowNodeKind.Condition,
        TargetPort = targetPort,
    };
}
