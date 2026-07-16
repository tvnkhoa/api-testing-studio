using ApiTestingStudio.Application.Abstractions;
using ApiTestingStudio.Application.Workflows;
using ApiTestingStudio.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ApiTestingStudio.Application.Tests;

public sealed class WorkflowCatalogServiceTests
{
    private readonly InMemoryWorkflowRepository _repository = new();
    private readonly Guid _workspaceId = Guid.NewGuid();

    private WorkflowCatalogService CreateService() => new(_repository);

    [Fact]
    public async Task Create_then_List_returns_the_workflow()
    {
        var service = CreateService();

        var created = await service.CreateAsync(_workspaceId, "Login flow");
        var list = await service.ListAsync(_workspaceId);

        created.IsSuccess.Should().BeTrue();
        list.Value.Should().ContainSingle(w => w.Id == created.Value.Id && w.Name == "Login flow");
    }

    [Fact]
    public async Task Create_with_blank_name_fails()
    {
        var service = CreateService();

        var result = await service.CreateAsync(_workspaceId, "   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowErrors.NameRequired);
    }

    [Fact]
    public async Task Rename_changes_the_name_and_preserves_the_workflow()
    {
        var service = CreateService();
        var created = await service.CreateAsync(_workspaceId, "Old");

        var result = await service.RenameAsync(created.Value.Id, "New");
        var list = await service.ListAsync(_workspaceId);

        result.IsSuccess.Should().BeTrue();
        list.Value.Should().ContainSingle(w => w.Id == created.Value.Id && w.Name == "New");
    }

    [Fact]
    public async Task Rename_missing_workflow_fails()
    {
        var service = CreateService();

        var result = await service.RenameAsync(Guid.NewGuid(), "New");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.not_found");
    }

    [Fact]
    public async Task Delete_removes_the_workflow()
    {
        var service = CreateService();
        var created = await service.CreateAsync(_workspaceId, "Temp");

        await service.DeleteAsync(created.Value.Id);
        var list = await service.ListAsync(_workspaceId);

        list.Value.Should().BeEmpty();
    }

    private sealed class InMemoryWorkflowRepository : IWorkflowRepository
    {
        private readonly Dictionary<Guid, Workflow> _store = [];

        public Task<Workflow?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(id, out var workflow) ? workflow : null);

        public Task<IReadOnlyList<WorkflowDefinition>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WorkflowDefinition> definitions = _store.Values
                .Where(w => w.WorkspaceId == workspaceId)
                .OrderBy(w => w.Name)
                .Select(w => new WorkflowDefinition { Id = w.Id, WorkspaceId = w.WorkspaceId, Name = w.Name, Description = w.Description })
                .ToList();
            return Task.FromResult(definitions);
        }

        public Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            _store[workflow.Id] = workflow;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.Remove(id);
            return Task.CompletedTask;
        }
    }
}
