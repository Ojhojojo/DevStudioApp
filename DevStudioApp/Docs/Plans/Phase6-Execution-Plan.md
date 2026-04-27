# Phase 6 Execution Plan (Minimal End-to-End)

This document tracks the first minimal Phase 6 slice: gather bounded repo context, generate a plan via Copilot CLI, allow human review/edit, and persist plan text on the work item.

## Objective

Deliver a practical planning workflow directly inside the work item dialog:
- generate a markdown implementation plan from current project/task context,
- review and edit it,
- save it back to `WorkItem.Plan`.

## Implementation status

| Area | Status | Notes |
|------|--------|-------|
| S1 Planning service contract | Done | `Services/PlanningService.cs` adds `IPlanningService` + `PlanningResult` |
| S2 Context gathering | Done | Captures branch, `git status --short`, top-level project files, and work item metadata |
| S3 Copilot plan generation | Done | Uses Copilot CLI with token preflight and bounded prompt/context |
| S4 Review/edit UI | Done | `WorkItemDialog.razor` adds Generate Plan, editable plan field, Save Plan action |
| S5 Persistence | Done | Plan saves through existing `WorkItemService.UpdateAsync` |
| S6 DI and logging | Done | `IPlanningService` registered in service collection and generation/save logging added |

## Validation checklist

- [x] User can generate a plan for an existing work item.
- [x] Generated plan is displayed in editable `WorkItem.Plan`.
- [x] User can save edited plan without closing dialog.
- [x] Build succeeds after integration.
- [ ] Manual runtime check on device: verify Copilot CLI availability, token setup, and expected output quality.

## Out of scope in this slice

- Vector store/semantic retrieval.
- Multi-provider model routing.
- Autonomous execution pipelines and retry orchestration.
