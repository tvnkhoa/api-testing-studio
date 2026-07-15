using ApiTestingStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiTestingStudio.Infrastructure.Persistence;

/// <summary>
/// EF Core context for a single workspace's SQLite database. Entities are mapped as a flat
/// relational model (foreign keys via <c>*Id</c> columns); navigation graphs and rich mapping
/// are added incrementally in later sprints. See <c>.claude/DATABASE_GUIDELINES.md</c>.
/// </summary>
public sealed class WorkspaceDbContext : DbContext
{
    public WorkspaceDbContext(DbContextOptions<WorkspaceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Endpoint> Endpoints => Set<Endpoint>();

    public DbSet<EndpointFolder> EndpointFolders => Set<EndpointFolder>();

    public DbSet<ProfileDefinition> Profiles => Set<ProfileDefinition>();

    public DbSet<EnvironmentDefinition> Environments => Set<EnvironmentDefinition>();

    public DbSet<Variable> Variables => Set<Variable>();

    public DbSet<WorkflowDefinition> Workflows => Set<WorkflowDefinition>();

    public DbSet<WorkflowNode> WorkflowNodes => Set<WorkflowNode>();

    public DbSet<WorkflowEdge> WorkflowEdges => Set<WorkflowEdge>();

    public DbSet<TestCaseDefinition> TestCases => Set<TestCaseDefinition>();

    public DbSet<Run> Runs => Set<Run>();

    public DbSet<RunStep> RunSteps => Set<RunStep>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<WorkspaceSetting> Settings => Set<WorkspaceSetting>();

    public DbSet<LogEntry> Logs => Set<LogEntry>();

    public DbSet<PackageMetadata> Packages => Set<PackageMetadata>();

    public DbSet<RequestHistoryEntry> RequestHistory => Set<RequestHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Workspace>().HasKey(x => x.Id);
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WorkspaceId);
        });

        modelBuilder.Entity<EndpointFolder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ServiceId);
            entity.HasIndex(x => x.ParentFolderId);
        });

        modelBuilder.Entity<Endpoint>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ServiceId);
            entity.HasIndex(x => x.FolderId);
        });
        modelBuilder.Entity<ProfileDefinition>().HasKey(x => x.Id);
        modelBuilder.Entity<EnvironmentDefinition>().HasKey(x => x.Id);
        modelBuilder.Entity<Variable>().HasKey(x => x.Id);
        modelBuilder.Entity<WorkflowDefinition>().HasKey(x => x.Id);

        // Workflow graph (Sprint 08): nodes/edges reference their owner by WorkflowId; the config/
        // mapping payloads are JSON TEXT columns. Enums (Kind) are stored as integers by convention.
        modelBuilder.Entity<WorkflowNode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WorkflowId);
        });

        modelBuilder.Entity<WorkflowEdge>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WorkflowId);
            entity.HasIndex(x => x.SourceNodeId);
            entity.HasIndex(x => x.TargetNodeId);
        });

        modelBuilder.Entity<TestCaseDefinition>().HasKey(x => x.Id);
        modelBuilder.Entity<Run>().HasKey(x => x.Id);
        modelBuilder.Entity<RunStep>().HasKey(x => x.Id);
        modelBuilder.Entity<Attachment>().HasKey(x => x.Id);
        modelBuilder.Entity<WorkspaceSetting>().HasKey(x => x.Id);
        modelBuilder.Entity<LogEntry>().HasKey(x => x.Id);

        modelBuilder.Entity<PackageMetadata>(entity =>
        {
            entity.HasKey(x => x.Id);
            // One dependency record per plugin within a workspace; upserts key off PluginId.
            entity.HasIndex(x => x.PluginId).IsUnique();
        });

        modelBuilder.Entity<RequestHistoryEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EndpointId);
        });
    }
}
