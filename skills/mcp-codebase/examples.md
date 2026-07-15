# Examples — Worked Reasoning, End to End

Read `skill.md` and `decision-tree.md` first. Each example shows the *reasoning*,
not just the calls. Bare tool names; confirm the observed prefix
(`mcp__codebase-index-local__…`) is present. Assume the trust-calibration read from
`skill.md` has already run.

---

## 1 — "What breaks if I change WorkflowEngine?" (the empty-result trap)

*Reasoning:* relationship question → graph. But this is a service, likely
DI-wired, so an empty result would mean "invisible," not "unused."

1. `orient({ intent: "what breaks if I change the WorkflowEngine", repoId })`
   → points to `find_impact_files` + `change_impact`, warns about DI wiring.
2. `find_impact_files({ repoId, filePath: ".../WorkflowEngine.cs", view: "surface" })`
   → empty `impactedFiles`. **Don't stop here.**
3. Suspect indirection → `search_regex({ repoId, pattern: "IWorkflowEngine", excludeTests: true })`
   → the DI registration + `IWorkflowEngine`-typed constructor params are the real
   consumers.
4. Report: "Consumers bind via `IWorkflowEngine` (DI); static graph shows no direct
   callers. Actual consumers: X, Y at file:line."

**Takeaway:** on a DI-wired type, empty = look at the interface, not "dead code."

---

## 2 — "Which tests do I run after editing?"

*Reasoning:* you've already edited, so refresh first, then let impact pick tests
instead of running the whole suite.

1. `index_repository({ repoId, repoPath, mode: "dirty" })` — cheap, changed files.
2. `change_impact({ repoId, profile: "compact" })` → ranked `testsToRun` +
   `residualRisk` for changed files with no linked test.
3. Run that subset; eyeball the `residualRisk` files by hand.

---

## 3 — "Where does a status value flow across services?"

*Reasoning:* the question spans repos — outside any single workspace, so built-in
tools can't answer it; this is the server's unique reach.

1. `get_value_contract_impact({ value: "resolved", column: "status" })` → hits
   across **all** registered repos, split into producers (writes) vs consumers
   (reads).
2. For symbol-level edges: `get_cross_repo_impact({ repoId, name, direction: "inbound" })`.
3. Plan the migration producer-first, consumer-by-consumer.

---

## 4 — "Implement a feature mirroring an existing one" (.NET)

*Reasoning:* understand the whole vertical slice in one call rather than reading
six files.

1. `get_feature_bundle({ repoId, seedSymbol: "ConversationNote",
   convention: "csharp-vertical-slice", includeSource: true })` → entity + config +
   commands + handlers + validators + queries + endpoints. Check `unresolvedRoles`
   for anything the name-pattern walk missed.
2. `Read` only the file(s) you'll actually template from.
3. Mirror the structure.

---

## 5 — "Understand an unfamiliar area fast" (graph-first, minimal reads)

*Reasoning:* navigate by meaning; open files only at the end, and only the ones
that matter. This is the loop to imitate.

1. `get_folder_summary({ repoId, folderPath: "src/.../Workflows" })` → per-file
   language, symbol count, caller count — the area's shape without reading it.
2. `search_symbols({ repoId, query: "workflow engine execute",
   strategy: "intent", ranked: true, limit: 5 })` → ranked entry candidates.
3. `get_symbol_context_pack({ repoId, name: "WorkflowEngine", profile: "compact" })`
   → callers, callees, importers in one call — usually enough to plan against.
4. `Read` the one or two files you're going to change. Not the neighborhood.

**Takeaway:** three graph calls replaced a directory crawl; you touched the
filesystem once, deliberately.

---

## 6 — "Audit user-facing text"

*Reasoning:* literal string content, and you want the enclosing symbol per hit —
that's `search_literals`, not Grep.

- `search_literals({ repoId, query: "timeout" })` → each literal with file, line,
  enclosing symbol (interpolated strings shown as `{...}`). Good for notification
  catalogs, error inventories, i18n sweeps.

---

## 7 — "Safe bulk rename" (propose, then commit)

*Reasoning:* mutation → never blind; preview gives blast radius and an undo path.

1. `rename_assist({ repoId, symbolId, newName, emitPreview: true })` → blast radius
   + applyable preview (`previewId` + `approvalToken`). Top-level identifiers need
   `includeLowConfidence: true` at apply.
2. Inspect the hunks.
3. `refactor_replace_apply({ previewId, approvalToken })` → **keep `rollbackId`**.
4. Wrong? `refactor_replace_rollback({ rollbackId })`.

---

## 8 — "EF persistence gotcha check"

*Reasoning:* persistence facts (converters, constraints, untranslatable
projections) live below the symbol graph.

- `get_persistence_mapping({ repoId, property: "HandledBy", ownerType: "Conversation" })`
  → column, converter, max length, CHECK constraints, plus a
  `DB_TRANSLATED_PROJECTION` warning if a value-converted property is used in an
  EF-translated `.Select()`/`.Where()` without prior materialization.

---

## 9 — Planning a change (Plan mode: read-only, cheap, non-mutating)

*Reasoning:* a plan estimates and proposes; it must not mutate, and it should hand
the implementer a map keyed to `symbolId`/`file:line`.

1. `get_symbol_context_pack({ name, profile: "nano" })` → what the symbol touches.
2. `find_impact_files` / `change_impact` (`compact`) → blast radius + tests the
   change will need.
3. `rename_assist` / `refactor_replace_preview` (dry-run) → estimate refactor size
   **without applying**.
4. Output a plan that names the exact symbols and lines to edit and the tests to
   run — so implementation acts without re-deriving the graph.

---

## 10 — Built-ins are the right call

*Reasoning:* match the tool to the shape — bytes or literal text, or no graph
available.

- "Show the current contents of `WorkflowEngine.cs`" → `Read` (authoritative bytes;
  never trust the index to the line).
- "Grep every `TODO(koi)`" → `Grep` (fast, always current). Use `search_regex` only
  when you also want the enclosing symbol.
- "Server not listed / stale and can't re-index" → Read/Grep/Glob, and tell the
  user the structural index is offline.

---

## 11 — Custom aggregate (SQL escape hatch)

*Reasoning:* no purpose-built tool answers it, but the graph tables do.

```sql
SELECT type, COUNT(*) AS n FROM edges WHERE repo_id = :repoId GROUP BY type ORDER BY n DESC
```

`query_graph({ repoId, sql })` — read-only, write/admin blocked, `:repoId` required.
Use for top fan-in symbols, orphan counts, route inventories, edge histograms.
