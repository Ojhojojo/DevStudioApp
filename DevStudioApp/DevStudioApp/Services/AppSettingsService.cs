using DevStudioDomain;
using DevStudioDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevStudioApp.Services;

public interface IAppSettingsService
{
    Task<AppSettings> GetAsync();
    Task<AppSettings> SaveAsync(AppSettings settings);
}

public class AppSettingsService(AppDbContext dbContext) : IAppSettingsService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AppSettings> GetAsync()
    {
        var settings = await _dbContext.AppSettings.FirstOrDefaultAsync(s => s.Id == 1);
        if (settings is not null)
        {
            return settings;
        }

        settings = new AppSettings();
        _dbContext.AppSettings.Add(settings);
        await _dbContext.SaveChangesAsync();
        return settings;
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings)
    {
        var existing = await GetAsync();
        existing.Theme = string.IsNullOrWhiteSpace(settings.Theme) ? "light" : settings.Theme.Trim();
        existing.LoggingLevel = string.IsNullOrWhiteSpace(settings.LoggingLevel) ? "Information" : settings.LoggingLevel.Trim();
        existing.DefaultWorktreeBasePath = settings.DefaultWorktreeBasePath?.Trim() ?? string.Empty;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return existing;
    }
}
