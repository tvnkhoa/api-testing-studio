using System.Text.Json;
using ApiTestingStudio.Application.Abstractions;

namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Persists Service Explorer expansion + selection state as JSON in the open workspace's
/// <c>Settings</c> table via <see cref="IWorkspaceSettingRepository"/>. Malformed or absent state
/// resolves to <see cref="ExplorerTreeState.Empty"/> so a corrupt value never breaks the panel.
/// </summary>
public sealed class ServiceExplorerStateService : IServiceExplorerStateService
{
    public const string StateKey = "explorer.tree-state";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkspaceSettingRepository _settings;

    public ServiceExplorerStateService(IWorkspaceSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task<ExplorerTreeState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await _settings.GetAsync(StateKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return ExplorerTreeState.Empty;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<StateDto>(json, SerializerOptions);
            return dto is null
                ? ExplorerTreeState.Empty
                : new ExplorerTreeState(dto.ExpandedIds ?? [], dto.SelectedId);
        }
        catch (JsonException)
        {
            return ExplorerTreeState.Empty;
        }
    }

    public async Task SaveAsync(ExplorerTreeState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        var dto = new StateDto { ExpandedIds = [.. state.ExpandedIds], SelectedId = state.SelectedId };
        var json = JsonSerializer.Serialize(dto, SerializerOptions);
        await _settings.SetAsync(StateKey, json, cancellationToken).ConfigureAwait(false);
    }

    private sealed class StateDto
    {
        public List<Guid>? ExpandedIds { get; init; }

        public Guid? SelectedId { get; init; }
    }
}
