# ADR-0010 — Secret storage: AES-256-GCM keyed by a DPAPI-wrapped master key

- **Status:** Accepted
- **Date:** 2026-07-16

## Context

Sprint 10 (Profiles & Environments) is the first feature that stores real secrets — profile
credentials (access/refresh tokens, passwords, API keys) and secret-typed variables. `CLAUDE.md`
mandates *secrets are always encrypted at rest — never persist plaintext*, and the product is
**100% offline** (no cloud KMS, no network). Phase 1 shipped a placeholder `ISecretProtector`
(`PlaceholderSecretProtector`) that only Base64-encoded values; it was never encryption and had to be
replaced. The seam (`ISecretProtector.Protect/Unprotect`, an Application port) already existed so no
call site persists plaintext.

## Decision

1. **AES-256-GCM for the payload.** `AesSecretProtector` (Infrastructure) encrypts each secret with
   `System.Security.Cryptography.AesGcm` (in-box, cross-platform). Output is self-describing:
   `base64( version(1) || nonce(12) || tag(16) || ciphertext )`. GCM is authenticated, so tampering
   fails on decrypt (surfaced as `CryptographicException`) rather than returning corrupt plaintext.
   A fresh random nonce per `Protect` means encrypting the same value twice yields different
   ciphertext.

2. **The AES master key is wrapped with Windows DPAPI.** `DpapiKeyStore` (`IKeyStore` port) generates
   a random 256-bit key on first use, wraps it via `ProtectedData.Protect(..., CurrentUser)` with an
   application-specific entropy value, and stores the wrapped blob at
   `%LocalAppData%/ApiTestingStudio/keys/master.key` — **outside** any workspace file. The DPAPI calls
   are marked `[SupportedOSPlatform("windows")]`; callers reach the key only through the platform-
   neutral `IKeyStore` interface, so Infrastructure keeps its `net10.0` TFM.

3. **Secrets stay inline as ciphertext** on the existing model — `Protected*` columns on
   `ProfileDefinition`, and `Value` on secret `Variable`s — rather than a separate `Secrets` table.
   This matches the `InitialCreate` schema and avoids a redundant table.

## Consequences

- **Portability trade-off (accepted):** DPAPI is user/machine-bound, so a workspace file's secrets
  cannot be decrypted on another machine or by another user. This is acceptable for an offline
  desktop app; a password-derived key for portable encrypted workspaces is a documented future
  improvement.
- The master key is cached in memory for the process lifetime and never logged. Losing/rotating the
  key file invalidates existing ciphertext (key rotation is future work).
- `System.Security.Cryptography.ProtectedData` is added to CPM (`Directory.Packages.props`) and
  referenced by Infrastructure only.

## Alternatives considered

- **DPAPI per field (no AES layer):** simpler but ties every ciphertext directly to DPAPI, precludes
  a future portable/password-based key, and bloats each value with DPAPI overhead.
- **Plaintext + file ACLs:** violates the constitution.
- **Password-derived key (PBKDF2/Argon2) now:** better portability but needs a master-password UX;
  deferred.
