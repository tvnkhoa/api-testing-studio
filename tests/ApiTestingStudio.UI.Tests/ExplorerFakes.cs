using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;
using ApiTestingStudio.UI.Services;

namespace ApiTestingStudio.UI.Tests;

/// <summary>Controllable <see cref="IServiceExplorerService"/> for view-model tests.</summary>
internal sealed class FakeServiceExplorerService : IServiceExplorerService
{
    public ServiceCatalogTree Tree { get; set; } = ServiceCatalogTree.Empty;

    public bool LoadShouldFail { get; set; }

    public List<ServiceDraft> CreatedServices { get; } = [];

    public List<Guid> DeletedServices { get; } = [];

    public Task<Result<ServiceCatalogTree>> LoadTreeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(LoadShouldFail
            ? Result.Failure<ServiceCatalogTree>(ServiceCatalogErrors.NoWorkspaceOpen)
            : Result.Success(Tree));

    public Task<Result<Service>> CreateServiceAsync(ServiceDraft draft, CancellationToken cancellationToken = default)
    {
        CreatedServices.Add(draft);
        return Task.FromResult(Result.Success(new Service { Name = draft.Name }));
    }

    public Task<Result<Service>> UpdateServiceAsync(Guid id, ServiceDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new Service { Id = id, Name = draft.Name }));

    public Task<Result> DeleteServiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DeletedServices.Add(id);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<EndpointFolder>> CreateFolderAsync(Guid serviceId, Guid? parentFolderId, string name, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new EndpointFolder { ServiceId = serviceId, ParentFolderId = parentFolderId, Name = name }));

    public Task<Result<EndpointFolder>> RenameFolderAsync(Guid id, string name, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new EndpointFolder { Id = id, ServiceId = Guid.NewGuid(), Name = name }));

    public Task<Result> DeleteFolderAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> ReorderServiceAsync(Guid id, bool up, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> ReorderFolderAsync(Guid id, bool up, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}

/// <summary>No-op <see cref="IEndpointCrudService"/> recording calls for view-model tests.</summary>
internal sealed class FakeEndpointCrudService : IEndpointCrudService
{
    public List<EndpointDraft> Created { get; } = [];

    public Task<Result<Endpoint>> CreateEndpointAsync(Guid serviceId, Guid? folderId, EndpointDraft draft, CancellationToken cancellationToken = default)
    {
        Created.Add(draft);
        return Task.FromResult(Result.Success(new Endpoint { ServiceId = serviceId, FolderId = folderId, Name = draft.Name, Path = draft.Path, Method = draft.Method }));
    }

    public Task<Result<Endpoint>> UpdateEndpointAsync(Guid id, EndpointDraft draft, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new Endpoint { Id = id, Name = draft.Name, Path = draft.Path, Method = draft.Method }));

    public Task<Result<Endpoint>> DuplicateEndpointAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(new Endpoint { Name = "copy", Path = "/x" }));

    public Task<Result> DeleteEndpointAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> MoveEndpointAsync(Guid id, Guid? targetFolderId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public Task<Result> ReorderEndpointAsync(Guid id, bool up, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}

/// <summary>In-memory <see cref="IServiceExplorerStateService"/>.</summary>
internal sealed class FakeExplorerStateService : IServiceExplorerStateService
{
    public ExplorerTreeState State { get; set; } = ExplorerTreeState.Empty;

    public int SaveCount { get; private set; }

    public Task<ExplorerTreeState> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(State);

    public Task SaveAsync(ExplorerTreeState state, CancellationToken cancellationToken = default)
    {
        State = state;
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Scripted <see cref="IDialogService"/> returning preset results.</summary>
internal sealed class FakeDialogService : IDialogService
{
    public ServiceDraft? ServiceResult { get; set; }

    public EndpointDraft? EndpointResult { get; set; }

    public string? NameResult { get; set; }

    public bool ConfirmResult { get; set; }

    public bool ImportWizardResult { get; set; }

    public int ShowImportWizardCount { get; private set; }

    public ServiceDraft? PromptService(string title, ServiceDraft? existing = null) => ServiceResult;

    public EndpointDraft? PromptEndpoint(string title, EndpointDraft? existing = null) => EndpointResult;

    public string? PromptName(string title, string label, string? existing = null) => NameResult;

    public ApiTestingStudio.Application.Profiles.ProfileDraft? PromptProfile(
        string title, ApiTestingStudio.Domain.Entities.ProfileDefinition? existing = null) => null;

    public (string Name, ApiTestingStudio.Domain.Enums.EnvironmentKind Kind)? PromptEnvironment(
        string title, ApiTestingStudio.Domain.Entities.EnvironmentDefinition? existing = null) => null;

    public ApiTestingStudio.Application.Variables.VariableDraft? PromptVariable(
        string title,
        ApiTestingStudio.Domain.Entities.Variable? existing,
        IReadOnlyList<ApiTestingStudio.Domain.Entities.EnvironmentDefinition> environments) => null;

    public ApiTestingStudio.Application.Testing.TestCaseDraft? PromptTestCase(
        string title,
        IReadOnlyList<ApiTestingStudio.Application.Testing.TestCaseTargetOption> targets,
        ApiTestingStudio.Application.Testing.TestCaseDraft? existing = null) => null;

    public ApiTestingStudio.Application.Testing.AssertionDraft? PromptAssertion(
        string title,
        IReadOnlyList<string> kinds,
        ApiTestingStudio.Application.Testing.AssertionDraft? existing = null) => null;

    public bool Confirm(string title, string message) => ConfirmResult;

    public bool ShowImportWizard()
    {
        ShowImportWizardCount++;
        return ImportWizardResult;
    }
}
