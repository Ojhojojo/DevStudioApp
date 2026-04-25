//using DevForge.AgenticStudio.Core.Events;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using DevStudioApp.Services;
//using DevStudioDomain;

//<<<<<<< TODO: Unmerged change from project 'DevStudioApp (net8.0-windows10.0.19041.0)', Before:
//=======
//using DevForge;
//using DevForge.AgenticStudio;
//using DevForge.AgenticStudio.Desktop;
//using DevForge.AgenticStudio.Desktop.Jobs;
//using DevStudioApp.Jobs;
//>>>>>>> After

//namespace DevStudioApp.Jobs;

//public class AgentPullRequestJob
//{
//    private readonly IGitWorktreeService _gitService;
//    private readonly IEventBus _eventBus;
//    private readonly AppDbContext _db;
//    private readonly ILogger<AgentPullRequestJob> _logger;

//    public AgentPullRequestJob(
//        IGitWorktreeService gitService,
//        AppDbContext db,
//        ILogger<AgentPullRequestJob> logger,
//        IEventBus eventBus)
//    {
//        _gitService = gitService;
//        _db = db;
//        _logger = logger;
//        _eventBus = eventBus;
//    }

//    public async Task ExecuteAsync(int itemId)
//    {
//        var item = await _db.KanbanItems
//            .Include(i => i.DevProject)
//            .FirstOrDefaultAsync(i => i.Id == itemId);

//        if (item == null || item.DevProject == null)
//        {
//            _logger.LogWarning("Item {ItemId} has no Project attached.", itemId);
//            await BroadcastStatusAsync(itemId, "❌ Error: No Project attached to this item.");
//            return;
//        }

//        if (string.IsNullOrEmpty(item.BranchName))
//        {
//            _logger.LogWarning("Item {ItemId} has no branch name saved.", itemId);
//            await BroadcastStatusAsync(itemId, "❌ Error: No branch name found for this item.");
//            return;
//        }

//        _logger.LogInformation("Starting PR creation for item {ItemId}, branch {BranchName}", itemId, item.BranchName);
//        await BroadcastStatusAsync(itemId, $"📤 Creating Pull Request for branch {item.BranchName}...");

//        try
//        {
//            // Get the worktree path (reconstruct it)
//            var worktreeName = $"feature-{itemId}-{item.Title.Replace(" ", "-").ToLowerInvariant()}";
//            var worktreePath = Path.Combine(item.DevProject.LocalPath, ".worktrees", worktreeName);

//            if (!Directory.Exists(worktreePath))
//            {
//                _logger.LogWarning("Worktree path not found at {WorktreePath}", worktreePath);
//                await BroadcastStatusAsync(itemId, "❌ Error: Worktree path not found.");
//                return;
//            }

//            // Update status to in-review
//            item.Status = "in-review";
//            item.UpdatedAt = DateTime.UtcNow;
//            await _db.SaveChangesAsync();

//            // Broadcast state change
//            await _eventBus.PublishItemStateChangedAsync(new ItemStateChangedEvent
//            {
//                KanbanItemId = itemId,
//                OldState = "waiting-for-review",
//                NewState = "in-review",
//                ChangedAt = DateTime.UtcNow
//            });

//            // Create the PR
//            var prResult = await _gitService.CreatePullRequestAsync(
//                item.DevProject,
//                worktreePath,
//                $"Feature: {item.Title}",
//                $"Implemented by DevForge Agent\n\nPlan:\n{item.AiPlan ?? item.Description}");

//            // Extract PR URL from result or log it
//            _logger.LogInformation("PR creation result for item {ItemId}: {Result}", itemId, prResult);

//            // Save PR result (you can extract URL if Azure DevOps returns it in the response)
//            // For now, just log that it was created
//            item.PullRequestUrl = "See Azure DevOps for details"; // Placeholder
//            await _db.SaveChangesAsync();

//            _logger.LogInformation("PR successfully created for item {ItemId}", itemId);
//            await BroadcastStatusAsync(itemId, $"✅ {prResult}\n\nThe item is now 'In Review'. Mark as 'Done' once the PR is merged.");

//            // Broadcast execution completed with PR info
//            await _eventBus.PublishExecutionCompletedAsync(new ExecutionCompletedEvent
//            {
//                KanbanItemId = itemId,
//                PullRequestUrl = item.PullRequestUrl,
//                Success = true,
//                CompletedAt = DateTime.UtcNow
//            });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "PR creation failed for item {ItemId}", itemId);
//            await BroadcastStatusAsync(itemId, $"❌ PR creation failed: {ex.Message}");

//            await _eventBus.PublishExecutionCompletedAsync(new ExecutionCompletedEvent
//            {
//                KanbanItemId = itemId,
//                Success = false,
//                ErrorMessage = ex.Message,
//                CompletedAt = DateTime.UtcNow
//            });
//        }
//    }

//    private async Task BroadcastStatusAsync(int itemId, string status)
//    {
//        // Event bus (for desktop)
//        await _eventBus.PublishAgentStatusUpdateAsync(new AgentStatusUpdateEvent
//        {
//            KanbanItemId = itemId,
//            Status = status,
//            UpdatedAt = DateTime.UtcNow
//        });
//    }
//}