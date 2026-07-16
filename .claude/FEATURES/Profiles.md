# Profiles

## Overview

A Profile simulates a **user / role** when calling APIs. Instead of manually editing headers to
test as different actors, the user selects a Profile and the platform supplies that actor's
credentials and authorization automatically. This is central to the product's role/permission
testing story: a workflow can run the same steps "as Admin", then "as Guest", and compare results.

Profiles hold sensitive material. **Secrets are always encrypted before storage** — the domain
persists ciphertext only; no code path may write plaintext.

## Scope / Capabilities

- Built-in role kinds: **Admin, Staff, Guest, Developer, Custom**.
- Each Profile stores: `AccessToken`, `RefreshToken`, `Username`, `Password`, `Email`, `ApiKey`,
  `Secret`, `Tenant`, `UserId`.
- **Run As → Admin** (etc.): a workflow, or an individual node, executes under a chosen Profile;
  the authorization header / API key is swapped in automatically.
- Profiles are Workspace-scoped and selectable per request, per workflow, or per node.
- Sensitive fields are protected at rest via `ISecretProtector`; they are never logged.

## Domain & Contracts

Domain record (`ApiTestingStudio.Domain`):

- `ProfileDefinition` — id, name, `ProfileRole` (enum: `Admin`, `Staff`, `Guest`, `Developer`,
  `Custom`), and the actor data. Sensitive values are stored **only** as `Protected*` fields:
  `ProtectedAccessToken`, `ProtectedRefreshToken`, `ProtectedPassword`, `ProtectedApiKey`,
  `ProtectedSecret` (ciphertext strings). Non-sensitive fields (`Username`, `Email`, `Tenant`,
  `UserId`) are stored as-is.

Application port (`ApiTestingStudio.Application`):

- `ISecretProtector` — `string Protect(string plaintext)` / `string Unprotect(string cipher)`.
  All sensitive values pass through it before persistence and only decrypt in-memory at call time.

Infrastructure implements `ISecretProtector` as `AesSecretProtector` (AES-256-GCM) keyed by a
DPAPI-wrapped master key from `DpapiKeyStore` (`IKeyStore`), registered in the Host. See ADR-0010.

**Auth scheme.** A profile's `ProfileKind` is a *role* archetype (Admin/Staff/Guest/…); how its
credentials become an outgoing authorization is a separate `AuthScheme` (`None`, `Bearer`, `Basic`,
`ApiKey`, `Custom`). `IAuthApplicator` (Application) maps the scheme to a header, decrypting the
needed `Protected*` field at call time: `Bearer {AccessToken}` / `Basic base64(user:pass)` /
`{ApiKeyHeaderName}: {ApiKey}`. It is applied by `RequestExecutionService` (API Runner, via an
optional `profileId`) and `RequestNodeHandler` (a workflow node's `ProfileId`) before the request
reaches the transport layer, which stays auth-agnostic.

Application services: `IProfileService` (CRUD; encrypts draft secrets on save — a null secret field
on update keeps the stored ciphertext, an empty string clears it).

## UI

- A **Profiles** manager: create/edit Profiles, choose role, enter credentials. Secret fields use
  masked inputs and are re-encrypted on save.
- A **Run As** selector on the request runner, workflow toolbar, and node property panel.
- MVVM (CommunityToolkit.Mvvm); Material Design; plaintext secrets never leave the edit control.

## Sprint

- **Sprint 10** — `ProfileDefinition`, `ISecretProtector` (AES/DPAPI), Run-As integration.

## Open Questions / Future

- Automatic token refresh using `RefreshToken` before/within a run.
- Login workflow that populates a Profile's tokens from a real auth call.
- Per-environment Profile overrides (different creds per Development/QA/Staging/Production).
- Optional master-password / key rotation for the secret store.
- Import of credentials from Postman Environments into Profiles.
