//using DevForge.AgenticStudio.Core.Dto;
//using DevForge.AgenticStudio.Core.Events;
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

//public class AgentPlanJob
//{
//    private readonly IAgentService _agentService;
//    private readonly IEventBus _eventBus;
//    private readonly AppDbContext _db;
//    private readonly ILogger<AgentPlanJob> _logger;

//    public AgentPlanJob(
//        IAgentService agentService,
//        AppDbContext db,
//        ILogger<AgentPlanJob> logger,
//        IEventBus eventBus)
//    {
//        _agentService = agentService;
//        _db = db;
//        _logger = logger;
//        _eventBus = eventBus;
//    }

//    public async Task ExecuteAsync(int itemId, string provider)
//    {
//        var item = await _db.KanbanItems.FindAsync(itemId);
//        if (item == null)
//        {
//            _logger.LogWarning("AgentPlanJob could not find item {ItemId}", itemId);
//            return;
//        }

//        var dto = new KanbanItemDto
//        {
//            Id = item.Id,
//            Title = item.Title,
//            Description = item.Description,
//            Status = item.Status
//        };

//        _logger.LogInformation("Starting AI planning for item {ItemId} using provider {Provider}", itemId, provider);
//        await BroadcastStatusAsync(itemId, "🤖 AI is planning...");

//        try
//        {
//            var plan = await _agentService.GeneratePlanAsync(dto, provider);

//            // Save plan to database
//            item.AiPlan = plan;
//            item.UpdatedAt = DateTime.UtcNow;
//            await _db.SaveChangesAsync();

//            // Broadcast via both SignalR (if available) and event bus
//            await BroadcastPlanGeneratedAsync(itemId, plan, provider);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error generating plan for item {ItemId}", itemId);
//            var errorMsg = $"Error generating plan: {ex.Message}";
//            item.AiPlan = errorMsg;
//            await _db.SaveChangesAsync();
//            await BroadcastPlanGeneratedAsync(itemId, errorMsg, provider);
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

//    private async Task BroadcastPlanGeneratedAsync(int itemId, string plan, string provider)
//    {
//        // Event bus (for desktop)
//        await _eventBus.PublishPlanGeneratedAsync(new PlanGeneratedEvent
//        {
//            KanbanItemId = itemId,
//            AiPlan = plan,
//            Provider = provider,
//            GeneratedAt = DateTime.UtcNow
//        });
//    }
//}