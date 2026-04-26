using DevStudioDomain;
using DevStudioDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace DevStudioApp.Services;

public interface IProjectService
{
    Task<List<Project>> ListAsync();
    Task<Project?> GetByIdAsync(int id);
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task DeleteAsync(int id);
    Task<int?> GetActiveProjectIdAsync();
    Task SetActiveProjectIdAsync(int? projectId);
}

public class ProjectService(AppDbContext dbContext) : IProjectService
{
    private const string ActiveProjectIdPreferenceKey = "active_project_id";
    private readonly AppDbContext _dbContext = dbContext;

    public Task<List<Project>> ListAsync()
    {
        return _dbContext.Projects
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public Task<Project?> GetByIdAsync(int id)
    {
        return _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateAsync(Project project)
    {
        var normalized = new Project
        {
            Name = project.Name.Trim(),
            LocalRepoPath = project.LocalRepoPath.Trim(),
            RemoteAzureRepoUrl = project.RemoteAzureRepoUrl.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Projects.Add(normalized);
        await _dbContext.SaveChangesAsync();
        return normalized;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        var existing = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id)
            ?? throw new InvalidOperationException($"Project with id {project.Id} was not found.");

        existing.Name = project.Name.Trim();
        existing.LocalRepoPath = project.LocalRepoPath.Trim();
        existing.RemoteAzureRepoUrl = project.RemoteAzureRepoUrl.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
        {
            return;
        }

        _dbContext.Projects.Remove(existing);
        await _dbContext.SaveChangesAsync();

        var activeProjectId = await GetActiveProjectIdAsync();
        if (activeProjectId == id)
        {
            await SetActiveProjectIdAsync(null);
        }
    }

    public Task<int?> GetActiveProjectIdAsync()
    {
        if (!Preferences.ContainsKey(ActiveProjectIdPreferenceKey))
        {
            return Task.FromResult<int?>(null);
        }

        var value = Preferences.Get(ActiveProjectIdPreferenceKey, 0);
        return Task.FromResult(value > 0 ? (int?)value : null);
    }

    public Task SetActiveProjectIdAsync(int? projectId)
    {
        if (projectId is null)
        {
            Preferences.Remove(ActiveProjectIdPreferenceKey);
        }
        else
        {
            Preferences.Set(ActiveProjectIdPreferenceKey, projectId.Value);
        }

        return Task.CompletedTask;
    }
}
