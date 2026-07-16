using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.ApiRunner;
using ApiTestingStudio.Application.Import;
using ApiTestingStudio.Application.ServiceCatalog;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Application.Workflows.Handlers;
using ApiTestingStudio.Application.Workspaces;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTestingStudio.Application.DependencyInjection;

/// <summary>
/// Registers application-layer use cases. Ports whose implementations live in Infrastructure
/// (storage, recent-workspaces, clock) are bound by <c>AddInfrastructure</c>; this method wires
/// only the pure application services.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        // Service Explorer (Sprint 05): tree read + service/folder CRUD, endpoint CRUD, per-workspace
        // tree state. These depend on the catalog repositories bound by AddInfrastructure.
        services.AddSingleton<IServiceExplorerService, ServiceExplorerService>();
        services.AddSingleton<IEndpointCrudService, EndpointCrudService>();
        services.AddSingleton<IServiceExplorerStateService, ServiceExplorerStateService>();

        // API Runner (Sprint 06): request execution + history. The IRequestExecutor and
        // IRequestHistoryRepository ports these depend on are bound by AddInfrastructure.
        services.AddSingleton<IRequestExecutionService, RequestExecutionService>();
        services.AddSingleton<IRequestHistoryService, RequestHistoryService>();

        // Import (Sprint 07): pure orchestration + format detection. The IEnumerable<IImporter> is
        // contributed by the Import.* plugins via AddPluginHost; the IDefinitionFetcher (URL fetch)
        // and ICatalogMerger (transactional merge) ports are bound by AddInfrastructure.
        services.AddSingleton<ISourceFormatDetector, SourceFormatDetector>();
        services.AddSingleton<IImportOrchestrator, ImportOrchestrator>();

        // Workflow engine (Sprint 08): headless graph execution. The RequestNodeHandler reuses the
        // IRequestExecutor from AddInfrastructure; IWorkflowRepository is also bound there. Run
        // history is in-memory this sprint (durable tables land in Sprint 13).
        services.AddSingleton<IVariableResolver, VariableResolver>();
        services.AddSingleton<IDelayScheduler, TaskDelayScheduler>();
        services.AddSingleton<INodeHandler, RequestNodeHandler>();
        services.AddSingleton<INodeHandler, ConditionNodeHandler>();
        services.AddSingleton<INodeHandler, LoopNodeHandler>();
        services.AddSingleton<INodeHandler, ParallelNodeHandler>();
        services.AddSingleton<INodeHandler, DelayNodeHandler>();
        services.AddSingleton<INodeHandlerRegistry, NodeHandlerRegistry>();
        services.AddSingleton<IWorkflowEngine, WorkflowEngine>();
        services.AddSingleton<IWorkflowRunStore, InMemoryWorkflowRunStore>();

        // Workflow Designer support (Sprint 09): pure services the UI designer consumes. The connector
        // validator and catalog CRUD are stateless singletons; the undo/redo service is editor state
        // (one per designer pane) and is therefore registered as transient in AddUi.
        services.AddSingleton<IConnectorValidator, ConnectorValidator>();
        services.AddSingleton<IWorkflowCatalogService, WorkflowCatalogService>();

        return services;
    }
}
