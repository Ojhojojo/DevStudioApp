using DevStudioDomain;
using DevStudioDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevStudioApp.Services;

public interface IWorkItemService
{
    Task<List<WorkItem>> ListByProjectAsync(int projectId, string? search = null);
    Task<WorkItem?> GetByIdAsync(int id);
    Task<WorkItem> CreateAsync(int projectId, string title, string description);
    Task<WorkItem> UpdateAsync(WorkItem item);
    Task<WorkItem> SetBranchNameAsync(int id, string branchName);
    Task DeleteAsync(int id);
    Task MoveAsync(int id, string targetStatus, int targetIndex);
}

public class WorkItemService(AppDbContext dbContext) : IWorkItemService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<List<WorkItem>> ListByProjectAsync(int projectId, string? search = null)
    {
        var query = _dbContext.WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(w =>
                EF.Functions.Like(w.Title, $"%{trimmed}%") ||
                EF.Functions.Like(w.Description, $"%{trimmed}%"));
        }

        return await query
            .OrderBy(w => w.Status)
            .ThenBy(w => w.Order)
            .ToListAsync();
    }

    public Task<WorkItem?> GetByIdAsync(int id)
    {
        return _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<WorkItem> CreateAsync(int projectId, string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var maxOrder = await _dbContext.WorkItems
            .Where(w => w.ProjectId == projectId && w.Status == KanbanStatuses.Backlog)
            .Select(w => (int?)w.Order)
            .MaxAsync() ?? -1;

        var item = new WorkItem
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Status = KanbanStatuses.Backlog,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();
        return item;
    }

    public async Task<WorkItem> UpdateAsync(WorkItem item)
    {
        var existing = await _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == item.Id)
            ?? throw new InvalidOperationException($"WorkItem with id {item.Id} was not found.");

        existing.Title = item.Title.Trim();
        existing.Description = item.Description ?? string.Empty;
        existing.Plan = item.Plan;
        existing.BranchName = item.BranchName;
        existing.PullRequestUrl = item.PullRequestUrl;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<WorkItem> SetBranchNameAsync(int id, string branchName)
    {
        var existing = await _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new InvalidOperationException($"WorkItem with id {id} was not found.");

        existing.BranchName = branchName.Trim();
        existing.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == id);
        if (existing is null)
        {
            return;
        }

        var projectId = existing.ProjectId;
        var status = existing.Status;
        var order = existing.Order;

        _dbContext.WorkItems.Remove(existing);

        var siblings = await _dbContext.WorkItems
            .Where(w => w.ProjectId == projectId && w.Status == status && w.Order > order)
            .ToListAsync();

        foreach (var sibling in siblings)
        {
            sibling.Order--;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task MoveAsync(int id, string targetStatus, int targetIndex)
    {
        if (!KanbanStatuses.All.Contains(targetStatus))
        {
            throw new ArgumentException($"Unknown status '{targetStatus}'.", nameof(targetStatus));
        }

        var item = await _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new InvalidOperationException($"WorkItem with id {id} was not found.");

        var sourceStatus = item.Status;
        var sourceOrder = item.Order;
        var projectId = item.ProjectId;

        if (sourceStatus == targetStatus)
        {
            var laneItems = await _dbContext.WorkItems
                .Where(w => w.ProjectId == projectId && w.Status == targetStatus)
                .OrderBy(w => w.Order)
                .ToListAsync();

            laneItems.Remove(laneItems.First(w => w.Id == id));
            var clamped = Math.Clamp(targetIndex, 0, laneItems.Count);
            laneItems.Insert(clamped, item);

            for (var i = 0; i < laneItems.Count; i++)
            {
                laneItems[i].Order = i;
            }
        }
        else
        {
            var sourceItems = await _dbContext.WorkItems
                .Where(w => w.ProjectId == projectId && w.Status == sourceStatus && w.Order > sourceOrder)
                .ToListAsync();
            foreach (var sibling in sourceItems)
            {
                sibling.Order--;
            }

            var targetLane = await _dbContext.WorkItems
                .Where(w => w.ProjectId == projectId && w.Status == targetStatus)
                .OrderBy(w => w.Order)
                .ToListAsync();

            var clamped = Math.Clamp(targetIndex, 0, targetLane.Count);
            foreach (var sibling in targetLane.Where(w => w.Order >= clamped))
            {
                sibling.Order++;
            }

            item.Status = targetStatus;
            item.Order = clamped;
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
