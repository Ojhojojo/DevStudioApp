using DevStudioApp.Services;
using DevStudioDomain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevStudioApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "devstudio.db");
            options.UseSqlite($"Data Source={dbPath}");
        });

        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IConnectivityTestService, ConnectivityTestService>();
        services.AddScoped<ISetupStatusService, SetupStatusService>();
        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<IWorkItemBranchService, WorkItemBranchService>();
        services.AddScoped<IWorkItemGitService, WorkItemGitService>();
        services.AddScoped<ICopilotCliService, CopilotCliService>();
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddSingleton<IActiveProjectStore, ActiveProjectStore>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();

        return services;
    }
}
