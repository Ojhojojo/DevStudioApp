# Phase 4 Sub-Plan: Branch Provision on Planned -> In Progress

This sub-plan captures a focused implementation slice for Phase 4: automatically creating a safe task branch when a work item transitions from `planned` to `in-progress`.

## Objective

When a user drags a card from `planned` to `in-progress`, the app should:
- create (or resolve) a task branch from the current checked-out branch,
- switch to that branch,
- and persist the final branch name to `WorkItem.BranchName`.

This prepares isolated branch context before Copilot-driven code changes.

## Locked Decisions

- Trigger: status transition `planned` -> `in-progress`.
- Branch format: `feature/{itemId}-{slugTitle}`.
- Base branch: current checked-out branch (`HEAD`) in the project repo.
- If branch name already exists (local or remote): append numeric suffix (`-2`, `-3`, ...).
- Persist selected branch name back into `WorkItem.BranchName`.

## Scope

### In scope
- Transition detection during board drag/drop flow.
- Branch name generation and slug normalization.
- Existence checks (local + remote), suffix conflict resolution.
- Branch creation and checkout.
- Save selected branch to `WorkItem`.
- Clear user feedback and structured logging.

### Out of scope (follow-up)
- Commit/push orchestration.
- PR creation.
- Multi-step AI execution pipeline.

## Implementation Outline

1. Add a small Git branch provision service (interface + implementation) in app services.
2. In board move flow, detect `planned` -> `in-progress`.
3. Run branch provision before/alongside status persistence (transactional behavior defined below).
4. Persist resulting branch name into the work item and return updated model.
5. Surface success/failure in snackbar and logs.

## Behavior Rules

- **Idempotency:** if work item already has a valid `BranchName`, do not recreate unless forced.
- **Conflict resolution:** probe candidate names until first available suffix.
- **Safety:** never delete or rewrite existing branches.
- **Error handling:** if branch creation fails, keep item in original status and show actionable error.

## Acceptance Criteria

- Moving `planned` -> `in-progress` creates/checks out a branch matching naming rules.
- Existing branch collision produces deterministic suffix branch.
- `WorkItem.BranchName` is populated with the actual selected branch name.
- Transition is blocked/reverted on branch provisioning failure.
- Logs include item id, source/target status, chosen branch, and failure reason.

## Test Checklist

- Happy path: unique title/id creates `feature/{id}-{slug}`.
- Collision path: existing branch creates suffixed name.
- Remote collision path: remote-only conflict also suffixes.
- Invalid title chars are slug-sanitized safely.
- Git failure (no repo, no remote, auth/network) shows readable error and does not silently continue.

## Suggested File Targets (implementation pass)

- `DevStudioApp/DevStudioApp/Components/Pages/Board.razor`
- `DevStudioApp/DevStudioApp/Services/` (new Git branch service)
- `DevStudioApp/DevStudioApp/MauiProgram.cs` (service registration)
- `DevStudioApp/DevStudioDomain/Entities/WorkItem.cs` (only if additional metadata is needed)

## Follow-up Linkage

This sub-plan should be referenced from:
- `Docs/Plans/Phase4-Execution-Plan.md` (main tracker for Phase 4),
- and any future `Phase4-FollowUp-Todos.md` for deferred items.
