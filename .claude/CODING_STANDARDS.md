# CODING_STANDARDS.md

These standards are enforced by `Directory.Build.props` (warnings-as-errors, analyzers) and
`.editorconfig`. If a rule is intentionally relaxed, it is documented in `.editorconfig` with a
reason.

## Language & build

- **.NET 10**, `LangVersion=latest`, nullable reference types **enabled**, implicit usings on.
- **TreatWarningsAsErrors = true.** A warning is a build failure. Fix the cause; do not blanket-
  suppress. Analyzer rules are tuned per-rule in `.editorconfig` with a justification comment.
- File-scoped namespaces. `System.*` usings first.

## Naming

| Element | Convention | Example |
|---|---|---|
| Types, methods, properties | PascalCase | `WorkspaceDbContext` |
| Interfaces | `I` + PascalCase | `IStorageProvider` |
| Private fields | `_camelCase` | `_dbContext` |
| Local variables, parameters | camelCase | `cancellationToken` |
| Async methods | `...Async` suffix | `ImportAsync` |
| Constants | PascalCase | `SchemaVersion` |

## MVVM

- ViewModels derive from `ObservableObject` (CommunityToolkit.Mvvm); use `[ObservableProperty]`
  and `[RelayCommand]` source generators.
- **Keep ViewModels small.** No business logic — delegate to Application services.
- **No business logic in code-behind.** Code-behind is limited to view-only concerns.
- Views bind to ViewModels; ViewModels are injected via DI.

## Dependency Injection

- Constructor injection only. No service locator; no `static` singletons for state.
- **Avoid static helper classes** (grab-bag utilities). DI extension methods
  (`AddApplication`, `AddInfrastructure`, `AddPluginHost`) are the sanctioned exception.
- Register services in the owning layer's `*ServiceCollectionExtensions`; compose them in Host.

## Async

- `async/await` end-to-end. Never block on async (`.Result`, `.Wait()`).
- Every async method takes a `CancellationToken` (default it if optional) and flows it through.
- Library/infra code uses `ConfigureAwait(false)`. UI code awaits on the UI context.

## Immutability & types

- Domain entities and DTOs are **immutable `record`s** with `init` accessors.
- Prefer `IReadOnlyList<T>` / `IReadOnlyDictionary<T>` on public surfaces.
- Model failure with `Result` / `Result<T>` (in `Shared`) for expected, recoverable errors;
  throw only for programmer errors / truly exceptional conditions.

## Error handling & logging

- Log through `ILogger<T>` (Serilog behind it). No `Console.WriteLine`.
- Catch broadly **only** at boundaries (plugin load, app startup, command handlers) and log.
- Never swallow exceptions silently. Never log secrets.

## Security

- Secrets (`AccessToken`, `RefreshToken`, `Password`, `ApiKey`, `Secret`) pass through
  `ISecretProtector` before persistence. The domain stores **ciphertext only**
  (`Protected*` fields). No code path may persist plaintext.

## Tests

- xUnit + FluentAssertions. Test names use `Method_Scenario_Expectation` (underscores — the
  CA1707 rule is disabled for `tests/`).
- Every layer has a test project. New behavior ships with tests.

## Code review checklist

- [ ] Does it respect the dependency table in `ARCHITECTURE.md`? (no illegal references)
- [ ] Build clean — **0 warnings**?
- [ ] Async all the way, `CancellationToken` threaded?
- [ ] ViewModels thin; no business logic in UI/code-behind?
- [ ] New capability behind an abstraction (plugin/port), not a concrete dependency?
- [ ] Secrets protected; nothing sensitive logged?
- [ ] Immutable records used for data types?
- [ ] Docs updated (`.claude/`) for any architectural change?
- [ ] Tests added/updated and green?
