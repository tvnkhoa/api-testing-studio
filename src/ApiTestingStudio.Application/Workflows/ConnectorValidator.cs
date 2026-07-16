using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Workflows;

/// <inheritdoc />
public sealed class ConnectorValidator : IConnectorValidator
{
    public Result Validate(ConnectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceNodeId == request.TargetNodeId)
        {
            return Result.Failure(WorkflowErrors.ConnectSelf);
        }

        if (!NodePortCatalog.OutputPorts(request.SourceKind).Contains(request.SourcePort, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(WorkflowErrors.ConnectUnknownPort(request.SourcePort));
        }

        if (!NodePortCatalog.InputPorts(request.TargetKind).Contains(request.TargetPort, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(WorkflowErrors.ConnectUnknownPort(request.TargetPort));
        }

        var duplicate = request.ExistingEdges.Any(e =>
            e.SourceNodeId == request.SourceNodeId
            && e.TargetNodeId == request.TargetNodeId
            && string.Equals(e.SourcePort, request.SourcePort, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.TargetPort, request.TargetPort, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return Result.Failure(WorkflowErrors.ConnectDuplicate);
        }

        return Result.Success();
    }
}
