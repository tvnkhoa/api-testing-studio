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
- [ ] Profile/Environment/Variable/Secret models + migration.
- [ ] AES + DPAPI secret protector replacing placeholder.
- [ ] Profile/environment services + variable resolution.
- [ ] Auth application in request execution.
- [ ] Manager UI + environment switcher + masked fields.
