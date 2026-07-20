using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.ViewModels.Stress;

/// <summary>
/// A selectable stress target in the config panel: a display <see cref="Label"/> plus the
/// discriminated identity the orchestrator needs (an endpoint or a workflow).
/// </summary>
public sealed record StressTargetOption(string Label, StressTargetKind Kind, Guid? EndpointId, Guid? WorkflowId);
