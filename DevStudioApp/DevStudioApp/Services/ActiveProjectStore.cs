using DevStudioDomain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DevStudioApp.Services;

public interface IActiveProjectStore
{
    Project? Current { get; }
    event Action? OnChange;
    Task InitializeAsync();
    Task SetActiveAsync(int? projectId);
    Task RefreshAsync();
}

public class ActiveProjectStore(IServiceScopeFactory scopeFactory) : IActiveProjectStore
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private Project? _current;

    public Project? Current => _current;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();

        var activeId = await projectService.GetActiveProjectIdAsync();
        _current = activeId.HasValue ? await projectService.GetByIdAsync(activeId.Value) : null;
        NotifyChange();
    }

    public async Task SetActiveAsync(int? projectId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();

        await projectService.SetActiveProjectIdAsync(projectId);
        _current = projectId.HasValue ? await projectService.GetByIdAsync(projectId.Value) : null;
        NotifyChange();
    }

    public async Task RefreshAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();

        if (_current is null)
        {
            var activeId = await projectService.GetActiveProjectIdAsync();
            _current = activeId.HasValue ? await projectService.GetByIdAsync(activeId.Value) : null;
        }
        else
        {
            _current = await projectService.GetByIdAsync(_current.Id);
        }

        NotifyChange();
    }

    private void NotifyChange() => OnChange?.Invoke();
}
