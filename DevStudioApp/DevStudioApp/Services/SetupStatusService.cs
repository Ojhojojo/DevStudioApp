using DevStudioDomain;
using Microsoft.EntityFrameworkCore;

namespace DevStudioApp.Services;

public sealed record SetupStatus(bool HasProject, bool HasCopilotToken, bool HasAzurePat)
{
    public bool IsComplete => HasProject && HasCopilotToken && HasAzurePat;
}

public interface ISetupStatusService
{
    Task<SetupStatus> GetStatusAsync();
}

public class SetupStatusService(AppDbContext dbContext, ITokenService tokenService) : ISetupStatusService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ITokenService _tokenService = tokenService;

    public async Task<SetupStatus> GetStatusAsync()
    {
        var hasProject = await _dbContext.Projects.AnyAsync();

        var hasCopilotToken = !string.IsNullOrWhiteSpace(await _tokenService.GetCopilotTokenAsync());
        var hasAzurePat = !string.IsNullOrWhiteSpace(await _tokenService.GetAzurePatAsync());

        return new SetupStatus(hasProject, hasCopilotToken, hasAzurePat);
    }
}
