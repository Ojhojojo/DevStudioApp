# Dev Agent Studio - Project Plan (Revised v4)

**Vision**  
A local-first MAUI Blazor Hybrid desktop app with per-project Kanban boards. GitHub Copilot CLI handles AI tasks. All Git operations (clone, worktrees, push, PRs) target Azure Repos using a **single global Azure PAT**. Everything stays in-process and offline-first where possible.

**Core Constraints**
- .NET 8 + MAUI Blazor Hybrid
- MudBlazor UI
- SQLite + EF Core 8
- SecureStorage for all tokens
- Vertical Slice + Clean Architecture
- Global tokens → per-project repo linking

## Execution Snapshot

- [x] Phase 1 complete
- [x] Phase 2 complete
- [x] Phase 3 complete (one optional markdown follow-up remains)
- [x] Phase 4 complete (happy-path git branch, commit, push)
- [x] Phase 5 complete (Copilot CLI service + context + streaming + task actions)
- [x] Phase 6 minimal slice complete (context + generate + review/edit + store)
- [ ] Phase 7+ not started

Execution references:
- [Phase3-Execution-Plan.md](./Phase3-Execution-Plan.md)
- [Phase4-Execution-Plan.md](./Phase4-Execution-Plan.md)
- [Phase6-Execution-Plan.md](./Phase6-Execution-Plan.md)

## Phase 1: Foundation & Project Setup (1-2 days)
- [x] MAUI Blazor Hybrid .NET 8 solution
- [x] MudBlazor + MudBlazor.Extensions
- [x] Serilog logging
- [x] Basic layout (MudBlazor theme, dark/light)
- [x] **Milestone**: App runs with shell layout (sidebar + top bar + main area)

## Phase 2: Storage, Global Settings & Project Management (2-3 days)
- [x] EF Core DbContext with migrations
- [x] Entities:
  - `AppSettings` (singleton record for global prefs)
  - `Project` (Id, Name, LocalRepoPath, RemoteAzureRepoUrl, CreatedAt, etc.)
- [x] `ITokenService` (SecureStorage) for:
  - `CopilotToken`
  - `AzurePat`
- [x] **Global Settings Page** (`/settings`):
  - Copilot CLI token (masked, test button that runs `copilot --version`)
  - Global Azure PAT (masked, test button that calls `git ls-remote` or Azure REST)
  - General preferences (theme, default worktree base path, logging level, etc.)
- [x] **Project Management Page** (`/projects`):
  - List of projects (MudDataGrid) with name, local path, remote URL preview
  - "New Project" button → form with:
    - Project Name
    - Local Repository Folder (MAUI FolderPicker)
    - Azure Repos URL[](https://dev.azure.com/...)
  - Edit / Delete project
- [x] Navigation:
  - **Left Drawer (MudDrawer)**: "Projects" section – list of projects (click to switch active project) + "New Project" button
  - **Top AppBar**: App title, current project name (with dropdown to switch), Settings gear icon, Help
  - **Main content**: Changes based on route / active project (Kanban when project selected)
- [ ] On first run: wizard forces creation of first project + token entry
- [x] **Milestone**: Can configure global tokens securely and manage multiple projects with local + Azure repo links

## Phase 3: Kanban UI Core (per-project)
- [x] Kanban board scoped to Active Project (draggable cards implemented)
- [x] Lanes: Backlog → Planned → In Progress → Review → Done
- [x] Task CRUD
- [x] Drag & drop, filtering, search (project-scoped)
- [x] **Milestone**: Persistent per-project Kanban fully functional
- [ ] Optional follow-up: rich MudEx markdown editor UX polish

## Phase 4: Git Integration & Safe Execution (Azure Repos)
- [x] `git` CLI wrapper baseline for branch provisioning
- [x] Trigger automation on `planned` -> `in-progress`
- [x] Branch format `feature/{itemId}-{slugTitle}` with suffix conflict handling
- [x] Persist selected branch to `WorkItem.BranchName`
- [x] Full automatic remote auth handling from global Azure PAT for happy-path push actions
- [x] Commit and push orchestration (task-level actions)
- [x] **Milestone**: Safe git operations against Azure Repos using stored global token (happy path)
- [ ] Future enhancement: in-app developer approval/rework workflow after commit/push

## Phase 5: Copilot CLI Integration
- [x] `ICopilotCliService` with global Copilot token from SecureStorage
- [x] Process execution + streaming output
- [x] Context injection: current project’s local repo path and task metadata
- [x] Task/work-item UI action to run Copilot from dialog
- [x] **Milestone**: Copilot CLI works from within tasks

## Phase 6: AI Planning & RAG Foundation
- [x] Per-project repo context gathering (bounded minimal context in planning service)
- [x] Plan generation with human review screen (work item dialog)
- [x] Plan storage linked to Project + Task (`WorkItem.Plan`)
- [x] **Milestone**: AI planning flow works per project (minimal slice)
- [ ] Future enhancement: richer RAG retrieval (vector/semantic search)

## Phase 7: Agent Execution Engine
- [ ] Background runner with state machine (per project/task)
- [ ] Pipeline: Plan → Copilot Code Gen → Build/Test → Commit → Push
- [ ] Human approval gates
- [ ] **Milestone**: End-to-end agent flow

## Phase 8: PR Creation
- [ ] Push to Azure remote
- [ ] Create draft PR via `az repos pr create` (CLI) or Azure DevOps REST SDK
- [ ] Link PR URL back to task
- [ ] **Milestone**: One-click PR creation

## Phase 9: Polish & UX
- [ ] Onboarding wizard
- [ ] Notifications, keyboard shortcuts
- [ ] Export/import
- [ ] Error handling & logging

## Phase 10: Future
- [ ] Local RAG (Ollama)
- [ ] Plugins, multi-agent, etc.

## Non-Functional
- [x] Tokens never plaintext
- [x] Global PAT reused everywhere (policy and current implementation direction)
- [x] Strong separation: Settings (global) vs Projects (repo-specific)

## Decision Log
- [x] Global Azure PAT only
- [x] Settings page = global tokens + prefs
- [x] Separate Project Management page
- [x] Navigation: Left drawer for projects, top bar for Settings
- [x] Storage: SQLite for projects/tasks, SecureStorage for tokens

---

This structure gives you a very natural desktop workflow:
- Open app → see your projects in sidebar
- Click a project → instantly in its Kanban
- Gear icon → global settings (tokens)
- New Project button → quick repo linking

Ready to move forward?

---

Initial Files are copied from a seperate project and will be refactored to fit the new architecture. 
The focus is on building a solid foundation in Phase 1 and 2 before implementing the more complex features. Each phase has clear milestones to ensure steady progress and maintain focus on core functionality.