//using DevStudioApp.Services;
//using DevStudioDomain;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;

//namespace DevStudioApp.Jobs;

//public class AgentExecutionJob
//{
//    private readonly IAgentService _agentService;
//    private readonly IGitWorktreeService _gitService;
//    private readonly IEventBus _eventBus;
//    private readonly AppDbContext _db;
//    private readonly ILogger<AgentExecutionJob> _logger;

//    public AgentExecutionJob(
//        IAgentService agentService,
//        IGitWorktreeService gitService,
//        AppDbContext db,
//        ILogger<AgentExecutionJob> logger,
//        IEventBus eventBus)
//    {
//        _agentService = agentService;
//        _gitService = gitService;
//        _db = db;
//        _logger = logger;
//        _eventBus = eventBus;
//    }

//    public async Task ExecuteAsync(int itemId, string provider)
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

//        _logger.LogInformation("Starting execution for item {ItemId} on project {ProjectName}", itemId, item.DevProject.Name);
//        await BroadcastStatusAsync(itemId, $"🚀 Starting execution for project: {item.DevProject.Name}");

//        try
//        {
//            // Ensure plan exists
//            if (string.IsNullOrEmpty(item.AiPlan))
//            {
//                await BroadcastStatusAsync(itemId, "📝 Generating plan with Copilot...");
//                var planDto = new KanbanItemDto
//                {
//                    Id = item.Id,
//                    Title = item.Title,
//                    Description = item.Description
//                };
//                item.AiPlan = await _agentService.GeneratePlanAsync(planDto, "github");
//                await _db.SaveChangesAsync();
//            }

//            // Create worktree inside the real project
//            await BroadcastStatusAsync(itemId, "📂 Creating git worktree...");
//            var worktreePath = await _gitService.CreateWorktreeForItemAsync(
//                itemId,
//                item.Title,
//                item.DevProject.LocalPath);

//            // Run Copilot Autopilot
//            var prompt = $@"
//You are an expert full-stack developer.
//Project: {item.DevProject.Name}

//Approved plan:
//{item.AiPlan}

//Implement the feature completely.
//Create or update all necessary files using best practices.
//When done, say 'TASK COMPLETE'.
//";

//            await BroadcastStatusAsync(itemId, "🤖 Running Copilot Autopilot...");
//            var output = await _gitService.RunCopilotAutopilotAsync(itemId, worktreePath, prompt);

//            await BroadcastStatusAsync(itemId, "✅ Committing changes...");
//            await _gitService.CommitWorktreeAsync(worktreePath, $"Copilot Autopilot: {item.Title}");

//            // Get the branch name and save it
//            var branchName = await _gitService.GetCurrentBranchAsync(worktreePath);
//            _logger.LogInformation("Got branch name {BranchName} for item {ItemId}", branchName, itemId);

//            // Push to origin
//            await BroadcastStatusAsync(itemId, "📤 Pushing branch to origin...");
//            await _gitService.PushBranchAsync(worktreePath, branchName);

//            // Update item status to waiting-for-review and save branch name
//            item.BranchName = branchName;
//            item.Status = "waiting-for-review";
//            item.UpdatedAt = DateTime.UtcNow;
//            await _db.SaveChangesAsync();

//            _logger.LogInformation("Execution completed for item {ItemId}, branch {BranchName} is ready for review", itemId, branchName);
//            await BroadcastStatusAsync(itemId, $"✅ Code is ready for review! Branch: {branchName}");

//            // Broadcast execution completed
//            await _eventBus.PublishExecutionCompletedAsync(new ExecutionCompletedEvent
//            {
//                KanbanItemId = itemId,
//                BranchName = branchName,
//                Success = true,
//                CompletedAt = DateTime.UtcNow
//            });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Execution error for item {ItemId}", itemId);
//            await BroadcastStatusAsync(itemId, $"❌ Execution failed: {ex.Message}");

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