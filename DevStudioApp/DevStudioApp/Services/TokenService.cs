using Microsoft.Maui.Storage;

namespace DevStudioApp.Services;

public interface ITokenService
{
    Task<string?> GetCopilotTokenAsync();
    Task SetCopilotTokenAsync(string? token);
    Task<string?> GetAzurePatAsync();
    Task SetAzurePatAsync(string? token);
}

public class TokenService : ITokenService
{
    private const string CopilotTokenKey = "copilot_token";
    private const string AzurePatKey = "azure_pat";

    public Task<string?> GetCopilotTokenAsync()
    {
        return SecureStorage.GetAsync(CopilotTokenKey);
    }

    public async Task SetCopilotTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            SecureStorage.Remove(CopilotTokenKey);
            return;
        }

        await SecureStorage.SetAsync(CopilotTokenKey, token.Trim());
    }

    public Task<string?> GetAzurePatAsync()
    {
        return SecureStorage.GetAsync(AzurePatKey);
    }

    public async Task SetAzurePatAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            SecureStorage.Remove(AzurePatKey);
            return;
        }

        await SecureStorage.SetAsync(AzurePatKey, token.Trim());
    }
}
