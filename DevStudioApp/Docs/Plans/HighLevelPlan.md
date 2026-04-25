# Dev Agent Studio - Project Plan (Revised v3)

**Vision**  
A local-first MAUI Blazor Hybrid desktop app with per-project Kanban boards. GitHub Copilot CLI handles AI tasks. All Git operations (clone, worktrees, push, PRs) target Azure Repos using a **single global Azure PAT**. Everything stays in-process and offline-first where possible.

**Core Constraints**
- .NET 8 + MAUI Blazor Hybrid
- MudBlazor UI
- SQLite + EF Core 8
- SecureStorage for all tokens
- Vertical Slice + Clean Architecture
- Global tokens → per-project repo linking

## Phase 1: Foundation & Project Setup (1-2 days)
- MAUI Blazor Hybrid .NET 8 solution
- MudBlazor + MudBlazor.Extensions
- Serilog logging
- Basic layout (MudBlazor theme, dark/light)
- **Milestone**: App runs with shell layout (sidebar + top bar + main area)

## Phase 2: Storage, Global Settings & Project Management (2-3 days)
- EF Core DbContext with migrations
- Entities:
  - `AppSettings` (singleton record for global prefs)
  - `Project` (Id, Name, LocalRepoPath, RemoteAzureRepoUrl, CreatedAt, etc.)
- `ITokenService` (SecureStorage) for:
  - `CopilotToken`
  - `AzurePat`
- **Global Settings Page** (`/settings`):
  - Copilot CLI token (masked, test button that runs `copilot --version`)
  - Global Azure PAT (masked, test button that calls `git ls-remote` or Azure REST)
  - General preferences (theme, default worktree base path, logging level, etc.)
- **Project Management Page** (`/projects`):
  - List of projects (MudDataGrid) with name, local path, remote URL preview
  - "New Project" button → form with:
    - Project Name
    - Local Repository Folder (MAUI FolderPicker)
    - Azure Repos URL[](https://dev.azure.com/...)
  - Edit / Delete project
- Navigation:
  - **Left Drawer (MudDrawer)**: "Projects" section – list of projects (click to switch active project) + "New Project" button
  - **Top AppBar**: App title, current project name (with dropdown to switch), Settings gear icon, Help
  - **Main content**: Changes based on route / active project (Kanban when project selected)
- On first run: wizard forces creation of first project + token entry
- **Milestone**: Can configure global tokens securely and manage multiple projects with local + Azure repo links

## Phase 3: Kanban UI Core (per-project)
- Kanban board scoped to Active Project (MudDataGrid or draggable cards)
- Lanes: Backlog → Planned → In Progress → Review → Done
- Task CRUD + Markdown editor
- Drag & drop, filtering, search (project-scoped)
- **Milestone**: Persistent per-project Kanban fully functional

## Phase 4: Git Integration & Safe Execution (Azure Repos)
- LibGit2Sharp + credential injection from global Azure PAT
- Or `git` CLI wrapper with `GIT_ASKPASS` / credential helper
- Per-task worktrees in a project-specific base folder
- Automatic remote handling from Project.RemoteAzureRepoUrl + global PAT
- Clone (if needed), branch, commit, push
- **Milestone**: Safe git operations against Azure Repos using stored global token

## Phase 5: Copilot CLI Integration
- `ICopilotCliService` with global Copilot token from SecureStorage
- Process execution + streaming output
- Context injection: current project’s local repo path
- **Milestone**: Copilot CLI works from within tasks

## Phase 6: AI Planning & RAG Foundation
- Per-project repo context gathering
- Plan generation with human review screen
- Plan storage linked to Project + Task
- **Milestone**: AI planning flow works per project

## Phase 7: Agent Execution Engine
- Background runner with state machine (per project/task)
- Pipeline: Plan → Copilot Code Gen → Build/Test → Commit → Push
- Human approval gates
- **Milestone**: End-to-end agent flow

## Phase 8: PR Creation
- Push to Azure remote
- Create draft PR via `az repos pr create` (CLI) or Azure DevOps REST SDK
- Link PR URL back to task
- **Milestone**: One-click PR creation

## Phase 8: Polish & UX
- Onboarding wizard
- Notifications, keyboard shortcuts
- Export/import
- Error handling & logging

## Phase 10: Future
- Local RAG (Ollama)
- Plugins, multi-agent, etc.

## Non-Functional
- Tokens never plaintext
- Global PAT reused everywhere
- Strong separation: Settings (global) vs Projects (repo-specific)

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