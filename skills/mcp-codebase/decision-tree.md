# Decision Tree — Routing by Reasoning

Read `skill.md` first for the mental model. This file is a fast lookup, not a set of
laws: it encodes the *questions to ask yourself*, and the tool each answer points
to. When the entry point is unclear, `orient(intent)` is itself a routing step —
let it classify and recommend, then come back here to refine.

Tool names are bare; the observed prefix is `mcp__codebase-index-local__…`. Confirm
a tool is present before calling.

## 0. Calibrate trust (not a ritual — a confidence weight)

```
list_repositories / health_check → indexed commitSha
   ├─ == HEAD ............... trust structural answers fully
   ├─ you edited since ...... index_repository(mode='dirty')   # cheap, changed files
   ├─ branch switched ....... index_repository(mode='full')    # purge stale entries
   └─ can't refresh ......... use text tools, report lower confidence
```

## 1. What SHAPE is the question?

```
Relationship / meaning (how code connects, what a change affects) ─> §2/§3/§4/§5 (graph)
Literal text (a string, a pattern, a doc) ────────────────────────> §6 (text tools)
Exact current bytes I will edit ──────────────────────────────────> Read (built-in)
```

The graph is the default for understanding. Drop to text/bytes only for the last
two shapes, or when the graph is blind (empty result → §9.3).

## 2. Locate & understand a symbol (navigate by meaning, read files last)

```
Know a concept, not a name .......... search_symbols(strategy='intent', ranked=true)
Know the identifier ................. search_symbols(strategy='name')
Grasp an area's shape ............... get_folder_summary   # cheaper than reading files
Understand one symbol to change it .. get_symbol_context_pack(name)   # callers+callees+importers, 1 call
Drill one resolved symbolId ......... get_symbol_detail
See its source ...................... get_symbol_source   (then Read only the file you'll edit)
Map a stack-trace line -> symbol .... find_symbol_at_line
Who last touched it ................. get_symbol_blame
```

## 3. Follow relationships — set, path, or tree?

```
Caller / callee SET ................. get_change_context
PATH between two points ............. get_call_chain(direction=callers|callees)
Whole outbound execution TREE ....... trace_execution_flow
Interface -> implementations ........ find_implementations
Field: who READS vs WRITES .......... find_field_accesses(mode=read|write|all)
Public API surface / entry points ... find_entry_points(kind=method|class|route_handler)
```

## 4. Impact & tests — where are you in the edit?

```
Before editing, scope a file ........ find_impact_files(view=files|surface)
Risk-rank a diff / PR ............... detect_changes(policy=quick-triage|strict-review|release-gate)
After editing, pick tests ........... change_impact   (ranked testsToRun + residualRisk)
Link a test to its source ........... link_tests_to_source
```

## 5. Dependencies, hygiene, cross-repo

```
Dependency edges .................... get_dependency_graph
Circular dependencies ............... detect_circular_dependencies(mode=module|symbol)
Likely dead code .................... dead_code_scan   ⚠ blind to DI/reflection (see §9.3)
Consumers of a NuGet package ........ find_package_consumers
Impact into OTHER repos ............. get_cross_repo_impact(direction=inbound|outbound)
Who produces/consumes a stored value  get_value_contract_impact   # data-contract migrations
```

## 6. Text, strings, docs (when the shape is literal)

```
Code pattern + enclosing symbol ..... search_regex
User-facing string content .......... search_literals
Documentation / doc drift ........... query_docs(mode=search|stale|coverage)
Fast purely-literal sweep ........... Grep (built-in)
```

## 7. .NET framework intelligence

```
EF: column / converter / constraints  get_persistence_mapping
ASP.NET routes -> handlers .......... route_map   (count:0 + hint on non-ASP.NET repos — take the hint)
Mirror a vertical-slice feature ..... get_feature_bundle(convention='csharp-vertical-slice')
```

## 8. Mutate — always propose before you commit

```
rename ............................. rename_assist(emitPreview=true) ┐
bulk literal/regex replace ......... refactor_replace_preview ───────┤
owner-scoped symbol migration ...... refactor_symbol_migration ──────┤ preview (dry-run)
promote literals -> enum ........... change_value_representation ────┘
                                          │ inspect hunks
                                          ▼
                                     refactor_replace_apply → keep rollbackId
                                          │ mistake?
                                          ▼
                                     refactor_replace_rollback(rollbackId)

In Plan mode: stop at preview — estimate blast radius, leave apply for implementation.
No purpose-built tool fits? ........ query_graph (read-only SQL, needs :repoId param)
```

## 9. When the graph is blind — degrade in this order

```
1. server/tool missing ............. Read/Grep/Glob (+ note index offline)
2. stale, can't refresh ............ text tools stay truthful; flag lower confidence
3. empty structural result ......... suspect DI/reflection/bus BEFORE "absent";
                                      confirm via search_regex on interface + registration
4. thin conceptual recall .......... widen search_symbols(intent) -> search_regex (no vectors)
5. framework tool gives a hint ..... follow the hint's tool, don't retry
6. about to mutate ................. Read real bytes; preview -> apply -> rollback
```
