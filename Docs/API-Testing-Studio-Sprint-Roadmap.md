# API Testing Studio - Sprint Roadmap

## Sprint 00 -- Architecture Validation & Technical Spike

### Goal

Validate the overall architecture before writing production code.

### Deliverables

-   Review Clean Architecture
-   Finalize Plugin Architecture
-   Define core interfaces
-   Define `.apistudio` package structure
-   Create initial ADRs
-   Risk assessment

### Acceptance Criteria

-   Architecture approved
-   Plugin contracts finalized
-   Solution dependency graph validated

------------------------------------------------------------------------

## Sprint 01 -- Foundation

### Goal

Bootstrap the solution.

### Scope

-   Solution structure
-   Clean Architecture
-   DI
-   Logging
-   Configuration
-   Theme
-   Build pipeline

### Acceptance Criteria

-   Solution builds successfully
-   No business features

------------------------------------------------------------------------

## Sprint 02 -- Workspace Storage

### Scope

-   SQLite
-   EF Core
-   Workspace lifecycle
-   Workspace metadata
-   Recent workspaces
-   Package metadata

------------------------------------------------------------------------

## Sprint 03 -- Plugin Infrastructure

### Scope

-   Plugin loader
-   Discovery
-   Registry
-   Version compatibility
-   Lifecycle
-   Base plugin contracts

------------------------------------------------------------------------

## Sprint 04 -- Shell UI

### Scope

-   WPF Shell
-   AvalonDock
-   Toolbar
-   Status bar
-   Theme
-   Layout persistence

------------------------------------------------------------------------

## Sprint 05 -- Service Explorer

### Scope

-   Service tree
-   Endpoint CRUD
-   Search
-   Context menu

------------------------------------------------------------------------

## Sprint 06 -- API Runner

### Scope

-   Request builder
-   Response viewer
-   Headers
-   Cookies
-   Timing
-   History

------------------------------------------------------------------------

## Sprint 07 -- Import System

### Scope

-   cURL
-   OpenAPI
-   Swagger URL
-   Scalar (.NET 9/.NET 10)
-   Postman
-   Auto detection
-   Import wizard

------------------------------------------------------------------------

## Sprint 08 -- Workflow Engine

### Scope

-   Execution engine
-   Nodes
-   Edges
-   Context
-   Loop
-   Parallel
-   Delay
-   Condition

------------------------------------------------------------------------

## Sprint 09 -- Workflow Designer

### Scope

-   Nodify
-   Canvas
-   Drag & Drop
-   Zoom
-   Minimap
-   Undo/Redo
-   Visual mapping

------------------------------------------------------------------------

## Sprint 10 -- Profiles & Environments

### Scope

-   Identity profiles
-   Secret storage
-   AES encryption
-   Variables
-   Environments

------------------------------------------------------------------------

## Sprint 11 -- Assertions & Test Cases

### Scope

-   Assertions
-   Test suites
-   JSON schema
-   Regex
-   Reporting

------------------------------------------------------------------------

## Sprint 12 -- Stress Runner

### Scope

-   Sequential
-   Loop
-   Concurrent
-   Metrics
-   TPS/RPS
-   P95/P99

------------------------------------------------------------------------

## Sprint 13 -- Dashboard & Logging

### Scope

-   Live dashboard
-   Charts
-   Timeline
-   Replay
-   Monitoring
-   Logs

------------------------------------------------------------------------

## Sprint 14 -- Packaging & Polish

### Scope

-   .apistudio package
-   Backup
-   Recovery
-   Performance
-   Optimization

------------------------------------------------------------------------

# Standard Sprint Template

Every sprint document should contain:

-   Goal
-   Scope
-   Requirements
-   Architecture Impact
-   Projects
-   Classes
-   Interfaces
-   Database Changes
-   Plugin Changes
-   UI Changes
-   Acceptance Criteria
-   Out of Scope
-   Risks
-   Future Improvements
-   Checklist

------------------------------------------------------------------------

# Standard Claude Code Sprint Prompt

``` text
You are now working on Sprint XX.

1. Read every document under `.claude/`.
2. Read all previous Sprint documents.
3. Follow Clean Architecture and Plugin Architecture.
4. Never break backward compatibility.
5. Update documentation if architecture changes.
6. Produce an implementation plan before writing code.
7. Keep the solution buildable.
8. Prefer extensibility over shortcuts.
9. Explain architectural trade-offs before changing them.
10. Verify acceptance criteria at the end of the sprint.
```
