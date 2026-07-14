# Events

Planned **in-process** application/domain events. The product is 100% offline; these are internal
notifications only — no external bus, no telemetry. They will be delivered through an in-process
**mediator / messenger** (e.g. CommunityToolkit.Mvvm `IMessenger` for UI-facing notifications, or a
lightweight application-layer dispatcher) introduced when the first publisher lands. Payloads are
immutable records. Until then this is the intended contract, not yet wired.

## Event catalogue

| Event | Payload | Publisher | Subscribers |
|---|---|---|---|
| **WorkspaceOpened** | `WorkspaceId`, `Name`, `Path` | `IWorkspaceService` (Application) | Shell / `ShellViewModel`, Explorer, Dashboard, status bar |
| **WorkspaceClosed** | `WorkspaceId` | `IWorkspaceService` | Shell, Explorer, Dashboard (clear/reset), Logs |
| **RunStarted** | `RunId`, `WorkflowId`/`SuiteId`, `StartedAt` | Workflow engine / test runner (Core) | Workflow Designer (node status), Dashboard, Logs |
| **RunStepCompleted** | `RunId`, `StepId`, `RunStatus`, `DurationMs` | Workflow engine / runner | Workflow Designer (live node state), Logs, Dashboard |
| **RunCompleted** | `RunId`, `RunStatus`, `TotalMs`, counts | Workflow engine / runner | Dashboard (aggregate refresh), Logs, status bar |
| **PluginRegistered** | `Name`, `Version`, `Assembly` | Plugin host (`AddPluginHost`, Core) | `IPluginRegistry` consumers, shell (tool windows/widgets), diagnostics |

## Notes

- **WorkspaceOpened / WorkspaceClosed** drive the whole shell lifecycle: the Explorer loads/clears
  its tree, the Dashboard rebuilds/clears its widgets, and the status bar updates.
- **RunStarted / RunStepCompleted / RunCompleted** form the run lifecycle stream. `RunStepCompleted`
  is what lets the Workflow Designer paint each node Pending → Running → Passed/Failed
  (`RunStatus`), and it feeds the Logs run tree and Dashboard timeline.
- **PluginRegistered** is raised as each `IPluginModule` is discovered and its services are
  registered; today the outcome is captured in `IPluginRegistry` (published to `MainViewModel` as a
  plugin count). A formal event is added when a mediator exists.
- Events are notifications, not commands: subscribers react, they do not mutate the publisher.
  Publishing is fire-and-forget on the app's context; subscribers must not block.

## Delivery

- **UI-facing** notifications (status bar, node status, widget refresh) suit the
  CommunityToolkit.Mvvm `IMessenger` already available to view models.
- **Application-layer** events that must not depend on the UI use a small in-process dispatcher in
  Core, keeping Clean Architecture's inward-only dependency rule intact.
- Either way delivery is synchronous in-process and offline; there is no external broker.

## Future

- Formalise the messenger abstraction and a typed event envelope.
- `AssertionEvaluated` / `StressSampleCollected` fine-grained progress events.
- Event log inspector for diagnostics.
