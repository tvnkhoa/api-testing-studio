# Commands

Planned application command surface. All UI commands are **`RelayCommand` / `AsyncRelayCommand`**
(CommunityToolkit.Mvvm) exposed by view models — never invoked from code-behind, never carrying
business logic (they delegate to Application-layer services). In the Phase 1 shell the
corresponding menu items exist but are **disabled**; each command is **stubbed until its sprint**
lands the backing feature. Triggers below are the intended menu / toolbar entry points.

## Command surface

| Command | Trigger | Sprint |
|---|---|---|
| **New Workspace** | File ▸ New Workspace | Sprint 02 |
| **Open Workspace** | File ▸ Open Workspace… | Sprint 02 |
| **Save Workspace** | File ▸ Save (Ctrl+S) | Sprint 02 |
| **Import…** | File ▸ Import… (import wizard) | Sprint 07 |
| **Export .apistudio** | File ▸ Export ▸ .apistudio | Sprint 14 |
| **Run Workflow** | Workflow Designer toolbar ▸ Run | Sprint 08 |
| **Run Test Suite** | Test Cases ▸ Run Suite | Sprint 11 |
| **Run Stress** | Endpoint/Workflow ▸ Run Stress | Sprint 12 |
| **Replay Run** | Dashboard / Logs ▸ Replay | Sprint 13 |

## Notes

- **New/Open/Save Workspace** delegate to `IWorkspaceService` and the active `IStorageProvider`
  (SQLite). Open/Save of the portable package use `IWorkspaceSerializer` (`.apistudio`).
- **Import…** opens the wizard and routes the chosen source through the matching `IImporter`
  (`curl` / `openapi` / `scalar` / `postman`) with auto-detection.
- **Export .apistudio** invokes `IExporter` (`apistudio`) to produce the ZIP package.
- **Run Workflow** hands the graph to the Sprint 08 engine, which dispatches nodes to
  `IWorkflowNode` implementations; status streams back to the designer.
- **Run Test Suite** evaluates assertions via `IAssertion` implementations and records results.
- **Run Stress** drives `IStressRunner` with a `StressPlan` and surfaces `StressMetrics`.
- **Replay Run** re-drives a persisted run tree for inspection (no new network calls unless the
  user opts to re-execute).
- Commands own their `CanExecute` state (e.g. Save disabled with no open workspace); long-running
  commands are async and honour a `CancellationToken`.

## Future

- Command palette / keyboard-first invocation.
- User-configurable shortcuts.
- Recent-workspaces submenu bound to workspace metadata.
