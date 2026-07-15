---
name: mcp-codebase
description: >
  Navigate and understand code through the codebase-index MCP server — a local,
  offline code-graph that knows symbols and their relationships (calls, imports,
  implementations, field reads/writes, dependencies) across many repositories.
  Reach for it whenever a question is about how code CONNECTS or what a change
  AFFECTS — "who calls / implements / reads this", "what breaks if I change X",
  "which tests cover this", "trace the flow", "is this used", "impact across
  repos" — or when orienting inside an unfamiliar codebase. It answers relational
  questions that text search cannot, and it lets you understand a repo without
  scanning files by hand. Use built-in Read/Grep/Glob as its complement: for the
  exact current bytes of a file you will edit, for a literal text pattern, or when
  the index is unavailable or stale.
---

# MCP Codebase Navigation

## The one idea

The server is a **graph of meaning and relationships** over the code, not a pile of
files. So your default when you need to *understand* code is to **navigate the
graph** — start from an intent or a symbol, follow its edges — rather than scan the
filesystem. Reading files and grepping text are the complement you drop to for two
specific reasons: you need the **exact current bytes** (because you're about to
edit), or the graph **can't see** what you're after (raw text, or code wired
dynamically). Reason from that trade-off and the specific tool almost always
follows.

This skill is self-contained; a freshly spawned subagent can act on it with no
prior conversation.

**Tool names.** The tools live under the server's configured prefix — observed here
as `mcp__codebase-index-local__<tool>`; bare names below (`search_symbols`, …) are
stable. Confirm a tool is actually present before relying on it; if the server
isn't there, you're in the degradation path (last section) — don't invent names.

## Three things to reason about, every time

**1. How much do you trust the graph right now?** It's a snapshot from the last
index run, not your working tree. A quick `list_repositories` / `health_check`
tells you the indexed `commitSha`; tools also self-report a `staleWarning`. If it
matches HEAD, trust structural answers fully. If you've since edited, a
`index_repository(mode='dirty')` refreshes just the changed files cheaply; a branch
switch needs `mode='full'`. If you can't refresh, lower your confidence and say so
rather than reporting stale edges as fact. The point isn't a ritual — it's knowing
how much weight your answer can bear.

**2. What shape is the question?** Three shapes, three homes:
- *Relationship / meaning* ("who calls this", "where does this concept live",
  "what depends on X") → the **graph**. This is the default and the reason the
  server exists.
- *Literal text* ("every `TODO(koi)`", a specific error string) → a **text tool**.
  Prefer `search_regex` / `search_literals` over raw Grep when you also want the
  enclosing symbol per hit; drop to `Grep` for a fast, purely literal sweep.
- *Exact bytes I'm about to change* → `Read`. The graph can be stale to the line;
  never edit from `get_symbol_source` alone.

**3. If the graph came back empty, is it really absent — or just invisible?**
Static edges don't capture dynamic wiring. A service resolved through its interface
(`services.AddScoped<IFoo, Foo>()`), reflection, or a message bus can have **zero**
static callers and empty impact while being used everywhere. So treat an empty
structural result as a *question*, not an answer: confirm by searching the
interface name and its registration before you conclude "unused" or "safe to
delete." `dead_code_scan` shares this blind spot by design.

## The semantic-navigation loop (your default)

When you need to understand an area or a symbol, prefer this over opening files:

1. **`orient(intent)`** when the entry tool is unclear — it classifies the intent,
   names the right tool(s), resolves seed symbols, and flags caveats. Let it route
   you instead of guessing.
2. **Locate by meaning:** `search_symbols` with `strategy='intent'` for a concept
   you can only describe ("send notification email"), `strategy='name'` when you
   know the identifier. Use `get_folder_summary` to grasp an area's shape without
   reading its files.
3. **Understand in one call:** `get_symbol_context_pack(name)` returns ranked
   candidates plus callers, callees, and importers together — usually enough to
   plan against without a single file read.
4. **Traverse only as far as the question needs** — `get_change_context` for a
   caller list, `get_call_chain` for a path, `trace_execution_flow` for the
   outbound sub-graph, `find_implementations` / `find_field_accesses` for the
   relationship you actually care about.
5. **Read last, and narrowly:** open the two or three files you're going to edit,
   not the neighborhood around them. Let the graph tell you which those are.

Note the *semantic* part: the "intent" strategy matches tokens over names and
signatures — it is not embedding similarity (the vector lane exists but is
unpopulated here). So describe intent in words that resemble identifiers, and if
recall is thin, widen with `search_regex`. Reason about it as smart lexical search,
not a mind reader.

## What each capability answers

Use `decision-tree.md` for the full routing. In brief:

| The question you're really asking | Where it's answered |
|---|---|
| "Where in this repo is the thing that does X?" | `search_symbols`, `get_folder_summary`, `get_file_summary` |
| "Explain this symbol so I can change it" | `get_symbol_context_pack`; `get_symbol_detail` for one known id |
| "Who calls / implements / reads-writes this?" | `get_change_context`, `find_implementations`, `find_field_accesses` |
| "How does control flow from here?" | `get_call_chain` (path), `trace_execution_flow` (sub-graph) |
| "What's the public surface / entry points?" | `find_entry_points` |
| "What breaks if I touch this file?" | `find_impact_files`; `detect_changes` to risk-rank a diff |
| "Which tests must I run for my edit?" | `change_impact`; `link_tests_to_source` |
| "Dependencies, cycles, dead code?" | `get_dependency_graph`, `detect_circular_dependencies`, `dead_code_scan` |
| "Does this ripple into other repos?" | `get_cross_repo_impact`, `get_value_contract_impact`, `find_package_consumers` |
| ".NET: how is this persisted / routed?" | `get_persistence_mapping`, `route_map` |
| "Build me a feature like this existing one" | `get_feature_bundle` |
| "Find this text / string / doc" | `search_regex`, `search_literals`, `query_docs` |
| "Rename or bulk-edit safely" | `rename_assist` → `refactor_replace_preview` → `_apply` (keep `rollbackId`) |
| "An aggregate no tool gives me" | `query_graph` (read-only SQL) |

## Choosing between tools that overlap

Each pair resolves to a single discriminating question:

- **`search_symbols` vs `search_regex` vs `search_literals`** — *what do I know?* A
  name or concept → `search_symbols`. A code pattern → `search_regex`. A piece of
  user-facing text → `search_literals`.
- **`get_change_context` vs `get_call_chain` vs `trace_execution_flow`** — *do I
  want a set, a path, or a tree?* Caller/callee **list** → `get_change_context`.
  The **path** linking two points → `get_call_chain`. The whole outbound
  **execution tree** → `trace_execution_flow`.
- **`get_symbol_context_pack` vs `get_symbol_detail`** — *am I exploring or
  confirming?* Exploring a name (may be ambiguous) → `context_pack`. Confirming one
  resolved `symbolId` → `get_symbol_detail`.
- **`find_impact_files` vs `change_impact` vs `detect_changes`** — *where am I in
  the edit?* Before editing, scoping a file → `find_impact_files`. After editing,
  choosing tests → `change_impact`. Reviewing a diff for risk → `detect_changes`.
- **A framework tool returned `count:0` with a hint** — that's a signal about the
  codebase, not a failure. `route_map` on a non-ASP.NET app is *correctly* empty;
  take its hint (e.g. `find_entry_points`) rather than retrying.

**Profiles are your token dial.** Every tool takes `profile`
(`nano`/`compact`/`standard`/`verbose`). Default low (`nano`/`compact`), especially
when planning or running as a subagent, and escalate only when the compact answer
left a real gap.

## Reasoning in Plan mode

Planning is read-only and should stay cheap and non-mutating:

- Favor the advisory, read-only tools — `orient`, `get_symbol_context_pack`,
  `find_impact_files`, `change_impact`, `detect_changes` — at `nano`/`compact`.
- `rename_assist` (default) and the `refactor_*_preview` tools are dry-run; use
  them to *estimate blast radius* in a plan. Leave the `_apply` step for
  implementation — a plan proposes, it doesn't mutate.
- Produce a plan that points at `symbolId`s and `file:line`, so the implementing
  step (or agent) can act without re-deriving the map.

## Running as a subagent

You may be spawned with only this skill and a task:

- Open with a freshness read, then `orient` the task intent; navigate the graph
  before touching files.
- Keep profiles low; you're optimizing the parent's context budget.
- Return **distilled conclusions with `file:line` / `symbolId` anchors**, not raw
  graph JSON. The parent wants the finding, not the transcript.
- If the server or a tool is absent, drop to the degradation path and note it in
  your result so the parent can calibrate.

## When the graph can't help — degrade deliberately

Not a fallback checklist to recite; a reasoning order:

1. **Server/tool missing** → the graph simply isn't an option; use Read/Grep/Glob
   and tell the user the structural index is offline.
2. **Index stale, can't refresh** → text tools stay truthful when the graph
   doesn't; prefer them and flag reduced confidence.
3. **Empty structural result** → suspect dynamic wiring before absence; confirm
   with `search_regex` on the interface and its registration.
4. **Conceptual query with thin recall** → widen from `search_symbols(intent)` into
   `search_regex`; don't expect vector-style recall that isn't there.
5. **About to mutate** → `Read` the real bytes first; for refactors, preview →
   inspect → apply → keep the `rollbackId` so a mistake is one call to undo.

See `examples.md` for worked end-to-end flows and `decision-tree.md` for detailed
routing.
