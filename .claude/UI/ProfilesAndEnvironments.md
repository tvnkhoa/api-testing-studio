# UI — Profiles & Environments

## Overview

The **Profiles & Environments** tool panel manages a workspace's identity profiles, environments,
and variables; the **toolbar environment switcher** selects the active environment. Both are MVVM
(CommunityToolkit) and depend only on Application ports (never Infrastructure). Secret values are
never displayed — they are entered masked and stored as ciphertext.

## Panel

- `ProfilesPanelViewModel : ToolPanelViewModel` (`ContentId = "tool.profiles"`), view
  `Views/Panels/ProfilesPanelView.xaml` — a `TabControl` with **Profiles / Environments / Variables**
  tabs, each a toolbar (New/Edit/Delete) over a `ListBox`. All actions delegate to
  `IProfileService` / `IEnvironmentService` / `IVariableService`; failures surface on the status bar.
- Registered in `AddUi`; added to `ShellViewModel.Tools`; DataTemplate in `Resources/PanelTemplates.xaml`;
  toggled via **View → Profiles & Environments** (`ToggleProfilesCommand`).

## Editors (modal dialogs, via `IDialogService`)

- **Profile editor** (`ProfileEditorViewModel` / `ProfileEditorDialog`) — name, role (`ProfileKind`),
  auth scheme (`AuthScheme`), API-key header name, actor fields, and five **masked secret** boxes
  (access/refresh token, password, API key, secret) with a **Reveal** toggle. Masking uses
  `Behaviors/PasswordBoxBehavior` (attached two-way binding for `PasswordBox.Password`); revealed
  mode swaps to a `TextBox` via a `DataTrigger`. On edit, a blank secret box keeps the stored
  ciphertext (never decrypted for display).
- **Environment editor** (`EnvironmentEditorViewModel` / `EnvironmentEditorDialog`) — name + kind.
- **Variable editor** (`VariableEditorViewModel` / `VariableEditorDialog`) — key, scope, value,
  `IsSecret`, and an environment picker shown only for the Environment scope.

## Environment switcher

- `EnvironmentSwitcherViewModel` — a toolbar `ComboBox` bound through
  `ShellViewModel.Environments`. Selecting an environment calls `IEnvironmentService.SetActiveAsync`;
  it refreshes on `EnvironmentsChangedMessage` (broadcast by the panel on create/rename/delete).

## Conventions

- Follows the `WorkflowsPanelViewModel` pattern (tool panel + list + commands + status-bar errors).
- Thin ViewModels; no business logic in code-behind (dialogs set only `DialogResult`).
- Material Design styling and the 4-based spacing scale.

## Sprint

- **Sprint 10** — panel, three editor dialogs, masked-secret behavior, toolbar switcher.
