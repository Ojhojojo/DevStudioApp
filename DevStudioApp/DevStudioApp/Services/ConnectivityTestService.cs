using System.Diagnostics;

namespace DevStudioApp.Services;

public interface IConnectivityTestService
{
    Task<string> TestCopilotCliAsync();
    Task<string> TestAzureRemoteAsync(string remoteUrl);
}

public class ConnectivityTestService : IConnectivityTestService
{
    public Task<string> TestCopilotCliAsync()
    {
        return RunProcessAsync("copilot", "--version");
    }

    public Task<string> TestAzureRemoteAsync(string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return Task.FromResult("Remote URL is required.");
        }

        return RunProcessAsync("git", $"ls-remote \"{remoteUrl.Trim()}\"");
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = (await standardOutputTask).Trim();
            var error = (await standardErrorTask).Trim();

            if (process.ExitCode == 0)
            {
                return string.IsNullOrWhiteSpace(output) ? "Success." : output;
            }

            return string.IsNullOrWhiteSpace(error) ? "Command failed." : error;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
