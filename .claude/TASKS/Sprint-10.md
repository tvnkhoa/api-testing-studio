# Sprint 10 — Profiles & Environments

## Goal
Add identity profiles, environments, and variables with real secret storage — replacing the Phase 1 `PlaceholderSecretProtector` with a genuine DPAPI/AES `ISecretProtector` — so requests and workflows resolve auth and config safely.

## Scope
- Identity/auth profiles (e.g. Bearer, Basic, API key, custom headers).
- Environments (named sets of variables; active-environment selection).
- Variables: workspace-, environment-, and profile-scoped; interpolation in requests/workflows.
- Real `ISecretProtector`: AES encryption with DPAPI-protected key (Windows), offline.
- Secure secret storage in the workspace DB (ciphertext only).

## Requirements
- Replace `PlaceholderSecretProtector` everywhere via DI without breaking callers.
- Secrets encrypted at rest; never logged; masked in UI.
- Variable resolution precedence documented and deterministic.
- Requests (S06) and workflow context (S08) resolve `{{variables}}` and apply profiles.

## Architecture Impact
- Establishes the secrets/crypto boundary in Infrastructure.
- Extends the variable resolver (S08) with environment/profile scopes.
- Auth application becomes a pluggable step in request execution.

## Projects (which solution projects change)
- Domain — Profile, Environment, Variable, Secret models.
- Application — profile/environment services, resolution + auth application.
- Infrastructure — `AesSecretProtector` / DPAPI key store, repositories, migration.
- UI — profiles/environments manager, environment switcher, secret fields.
- Tests: Infrastructure.Tests (crypto), Application.Tests.

## Classes
- `IdentityProfile`, `Environment`, `Variable`, `Secret`.
- `AesSecretProtector` (replaces `PlaceholderSecretProtector`), `DpapiKeyStore`.
- `ProfileService`, `EnvironmentService`, `VariableResolver` (extend), `AuthApplicator`.
- `EnvironmentSwitcherViewModel`, `ProfileEditorViewModel`.

## Interfaces
- `ISecretProtector` (existing — real implementation now).
- `IProfileService`, `IEnvironmentService`, `IAuthApplicator`.
- `IKeyStore`.

## Database Changes
- New tables: `Profiles`, `Environments`, `Variables`, `Secrets` (encrypted blob + metadata).
- Migration: `AddProfilesAndEnvironments`.

## Plugin Changes
- None required. (Auth scheme plugins are a possible future extension point.)

## UI Changes
- Profiles & Environments manager panel/dialogs.
- Active-environment switcher in toolbar/status bar.
- Masked secret input fields with reveal toggle.

## Acceptance Criteria
- Create profiles/environments/variables; switch active environment.
- A request uses profile auth and resolves environment variables correctly.
- Secrets are stored encrypted (verified: no plaintext in DB) and masked in UI.
- `AesSecretProtector` round-trips encrypt/decrypt; DPAPI key isolation works.
- No remaining references to `PlaceholderSecretProtector`.

## Out of Scope
- OAuth2 interactive flows / token refresh servers (offline constraint).
- Cross-machine secret portability (DPAPI is machine/user-bound).
- Cloud secret managers.

## Risks
- DPAPI machine/user binding breaks portability of workspace files (document clearly).
- Crypto correctness and key rotation strategy.
- Variable precedence ambiguity across scopes.

## Future Improvements
- Password-based key derivation for portable encrypted workspaces.
- OAuth2 device/client-credentials support.
- Secret sharing/vault integration.

## Checklist
- [x] Profile/Environment/Variable models + migration. (Domain records already existed from
  `InitialCreate`; migration `AddProfileAuthAndEnvironmentBinding` adds `Profiles.AuthScheme`/
  `ApiKeyHeaderName`, `Variables.EnvironmentId` + indexes, schema **v6**. No separate `Secrets` table —
  secrets stay inline as ciphertext.)
- [x] AES + DPAPI secret protector replacing placeholder. (`AesSecretProtector` AES-256-GCM +
  `DpapiKeyStore`/`IKeyStore`; `PlaceholderSecretProtector` deleted; ADR-0010.)
- [x] Profile/environment services + variable resolution. (`ProfileService`/`EnvironmentService`/
  `VariableService`; `VariableScopeSeeder` seeds Global→Workspace→Environment(active) into the context
  the existing `VariableResolver` consumes.)
- [x] Auth application in request execution. (`AuthApplicator` Bearer/Basic/ApiKey; applied in
  `RequestExecutionService` (optional `profileId`) and `RequestNodeHandler` (node `ProfileId`).)
- [x] Manager UI + environment switcher + masked fields. (`ProfilesPanelViewModel` tabbed panel +
  Profile/Environment/Variable editor dialogs with masked-secret reveal (`PasswordBoxBehavior`) +
  toolbar `EnvironmentSwitcherViewModel`.)

## Deviations from the original plan
- **No separate `Secret` entity/table.** Secrets persist inline as ciphertext (`Protected*` on
  `ProfileDefinition`, `Value` on secret `Variable`s), matching the `InitialCreate` schema and
  `FEATURES/Profiles.md`. A dedicated `Secrets` table would duplicate the model.
- **Naming follows existing code:** `ProfileDefinition`/`ProfileKind` (not the doc's `IdentityProfile`/
  `ProfileRole`). Auth *scheme* is a new `AuthScheme` enum, distinct from the `ProfileKind` role.
- **Migration is additive columns, not new tables** (the tables predate this sprint), named
  `AddProfileAuthAndEnvironmentBinding`; schema bump **5 → 6**.
- **Active environment persisted via `Settings`** (`active-environment-id`), not an `IsActive` column.
- **Variable resolution was not rewritten:** the S08 `VariableResolver` is unchanged; Sprint 10 only
  enriches the variables seeded into its context (`VariableScopeSeeder`). Dynamic tokens
  (`$guid`/`$now`) and `{{...}}` autocomplete remain future work.
- Delivered in **two phases** (backend/crypto first, then UI); both build at 0 warnings with tests.
