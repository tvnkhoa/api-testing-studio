# MCP Discovery & Tool Selection Workflow

## Objective

Before any task execution, automatically discover the available MCP tools and determine whether any of them provide a better capability than the built-in tools.

This workflow applies to the main planner and every automatically created subagent.

---

## Discovery Phase

Before performing any investigation, implementation, or analysis:

1. Inspect all available MCP servers and their exposed tools.
2. Identify each tool's purpose and capabilities.
3. Determine whether an MCP tool can complete the requested task more accurately or efficiently than built-in Claude tools.
4. Build an execution plan using the discovered capabilities.

Never assume a specific MCP server name or tool name.
Always discover first.

---

## Tool Selection Priority

For every task, choose the most specialized tool available.

Priority:

1. Specialized MCP tools
2. Repository-aware tools
3. Built-in Read/Edit
4. Built-in Grep/Find/Glob
5. Generic filesystem search

If an MCP provides equivalent or better functionality than a built-in tool, prefer the MCP.

---

## Source Code Discovery

When locating:

- classes
- interfaces
- methods
- symbols
- references
- implementations
- inheritance
- dependencies
- architecture
- call hierarchy

First determine whether an MCP provides semantic code search.

If available:

- use the MCP
- inspect returned files
- continue navigation using MCP capabilities

Only fall back to Grep/Find/Glob if:

- no suitable MCP exists
- the MCP explicitly cannot answer
- searching non-source assets
- searching generated files
- locating configuration files unsupported by the MCP

---

## Planning

Before creating subagents:

Evaluate whether any discovered MCP should become the primary investigation tool.

If yes:

- document which MCP will be used
- explain why it is preferred
- instruct every relevant subagent to use it first

---

## Subagent Workflow

Every subagent follows:

Step 1
Discover available MCP tools.

Step 2
Select the most specialized tool.

Step 3
Perform semantic discovery.

Step 4
Read relevant files.

Step 5
Continue investigation.

Step 6
Only use filesystem search as a fallback.

Subagents must not immediately begin with Grep or Find.

---

## Implementation Workflow

For implementation tasks:

Discover tools
→ Select MCP
→ Investigate
→ Design
→ Implement
→ Validate
→ Summarize

---

## Research Workflow

Discover tools
→ Semantic search
→ Reference tracing
→ Read implementation
→ Produce findings

---

## Architecture Workflow

Discover tools
→ Trace dependencies
→ Build component graph
→ Analyze architecture
→ Produce recommendations

---

## Failure Handling

If no MCP is suitable:

- explain why
- choose the best built-in tool
- continue execution

Never fail simply because an MCP is unavailable.

---

## Guiding Principle

The planner and every subagent should continuously evaluate whether a newly discovered MCP provides a better solution than the current tool.

The goal is to use the most capable tool available rather than defaulting to built-in filesystem search.