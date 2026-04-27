# Phase 3 follow-up (owner tasks)

This supplements the recorded execution plan: [Phase3-Execution-Plan.md](./Phase3-Execution-Plan.md). Use it to track work that is intentionally left for a follow-up pass after the main implementation.

## User / owner checklist

- [ ] **MudEx markdown description (two-way bind)**  
  In `DevStudioApp/Components/Dialogs/WorkItemDialog.razor`, the task description is currently a multiline `MudTextField` so the project builds cleanly and ships markdown as plain text.  
  **Your task:** Replace that field with the MudBlazor.Extensions markdown + preview control used in the plan (`MudExMarkdownEditPreview` or the name/API shipped by the referenced `MudBlazor.Extensions` version), and fix the two-way data binding the compiler was rejecting (correct parameter names, `@bind-Value` vs `Value` + `ValueChanged`, or `StringValue` as required by the component).  
  Confirm `AddMudExtensions()` remains in `MauiProgram.cs` and that `_Imports.razor` includes the extension namespaces. Optionally add the `mudBlazorExtensions.min.css` link in `wwwroot` if styles do not load.

- [ ] **Folder picker on non-Windows targets** (optional)  
  `FolderPicker` comes from [Community Toolkit.Maui](https://learn.microsoft.com/dotnet/communitytoolkit/maui/essentials/folder-picker). After enabling other platforms, verify Android storage permissions and any Windows capability notes from the toolkit docs if you hit runtime issues.

---

When the markdown item is done, you can remove the comment above the `MudTextField` in `WorkItemDialog.razor` and delete or check off the corresponding line in this file.
