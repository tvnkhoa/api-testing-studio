using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="IWorkflowRepository"/> operating on the open workspace's database. A short-lived
/// context is created per operation from the session's connection string, mirroring
/// <see cref="ServiceRepository"/>. The graph is stored across the <c>Workflows</c>/<c>WorkflowNodes</c>/
/// <c>WorkflowEdges</c> tables and hydrated into a runtime <see cref="Workflow"/> aggregate.
/// </summary>
public sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly WorkspaceSession _session;

    public WorkflowRepository(WorkspaceSession session)
    {
        _session = session;
    }

    public async Task<Workflow?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var definition = await context.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return null;
        }

        var nodes = await context.WorkflowNodes
            .AsNoTracking()
            .Where(n => n.WorkflowId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var edges = await context.WorkflowEdges
            .AsNoTracking()
            .Where(e => e.WorkflowId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new Workflow
        {
            Id = definition.Id,
            WorkspaceId = definition.WorkspaceId,
            Name = definition.Name,
            Description = definition.Description,
            Nodes = nodes,
            Edges = edges,
        };
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Workflows
            .AsNoTracking()
            .Where(w => w.WorkspaceId == workspaceId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        await using var context = CreateContext();

        var definition = new WorkflowDefinition
        {
            Id = workflow.Id,
            WorkspaceId = workflow.WorkspaceId,
            Name = workflow.Name,
            Description = workflow.Description,
        };

        var exists = await context.Workflows
            .AsNoTracking()
            .AnyAsync(w => w.Id == workflow.Id, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            context.Workflows.Update(definition);

            // Replace the child graph wholesale so removed nodes/edges do not linger.
            var oldNodes = await context.WorkflowNodes
                .Where(n => n.WorkflowId == workflow.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            context.WorkflowNodes.RemoveRange(oldNodes);

            var oldEdges = await context.WorkflowEdges
                .Where(e => e.WorkflowId == workflow.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            context.WorkflowEdges.RemoveRange(oldEdges);
        }
        else
        {
            await context.Workflows.AddAsync(definition, cancellationToken).ConfigureAwait(false);
        }

        var nodes = workflow.Nodes.Select(n => n with { WorkflowId = workflow.Id }).ToList();
        var edges = workflow.Edges.Select(e => e with { WorkflowId = workflow.Id }).ToList();
        await context.WorkflowNodes.AddRangeAsync(nodes, cancellationToken).ConfigureAwait(false);
        await context.WorkflowEdges.AddRangeAsync(edges, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();

        var edges = await context.WorkflowEdges
            .Where(e => e.WorkflowId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.WorkflowEdges.RemoveRange(edges);

        var nodes = await context.WorkflowNodes
            .Where(n => n.WorkflowId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        context.WorkflowNodes.RemoveRange(nodes);

        var definition = await context.Workflows
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (definition is not null)
        {
            context.Workflows.Remove(definition);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private WorkspaceDbContext CreateContext()
    {
        if (_session.ConnectionString is not { } connectionString)
        {
            throw new InvalidOperationException("Cannot access workflows: no workspace is open.");
        }

        return WorkspaceContextFactory.Create(connectionString);
    }
}
