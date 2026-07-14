using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>Editable fields captured by the new/edit service dialog.</summary>
public sealed record ServiceDraft(string Name, string? BaseUrl = null, string? Description = null);

/// <summary>Editable fields captured by the new/edit endpoint dialog.</summary>
public sealed record EndpointDraft(string Name, HttpVerb Method, string Path, string? Description = null);
