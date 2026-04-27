using System.Diagnostics;
using System.Text;
using DevStudioDomain.Entities;
using Microsoft.Extensions.Logging;

namespace DevStudioApp.Services;

public interface IWorkItemGitService
{
    Task<string> CommitWorkItemChangesAsync(WorkItem item, string commitMessage);
    Task<string> PushWorkItemBranchAsync(WorkItem item);
}

public class WorkItemGitService(
    IProjectService projectService,
    ITokenService tokenService,
    ILogger<WorkItemGitService> logger) : IWorkItemGitService
{
    private readonly IProjectService _projectService = projectService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<WorkItemGitService> _logger = logger;

    public async Task<string> CommitWorkItemChangesAsync(WorkItem item, string commitMessage)
    {
        if (string.IsNullOrWhiteSpace(commitMessage))
        {
            throw new InvalidOperationException("Commit message is required.");
        }

        var (project, repoPath) = await ResolveRepoAsync(item);
        EnsureBranchName(item);
        await EnsureOnBranchAsync(repoPath, item.BranchName!);

        await RunGitAsync(repoPath, "add -A");
        var status = await RunGitAsync(repoPath, "status --short");
        if (string.IsNullOrWhiteSpace(status))
        {
            return "No changes to commit.";
        }

        await RunGitAsync(repoPath, $"commit -m \"{EscapeArg(commitMessage.Trim())}\"");
        _logger.LogInformation("Committed changes for WorkItem {WorkItemId} on {BranchName}", item.Id, item.BranchName);
        return $"Committed on {item.BranchName}";
    }

    public async Task<string> PushWorkItemBranchAsync(WorkItem item)
    {
        var (project, repoPath) = await ResolveRepoAsync(item);
        EnsureBranchName(item);
        await EnsureOnBranchAsync(repoPath, item.BranchName!);
        await EnsureOriginRemoteAsync(repoPath, project.RemoteAzureRepoUrl);

        var pat = await _tokenService.GetAzurePatAsync();
        if (string.IsNullOrWhiteSpace(pat))
        {
            throw new InvalidOperationException("Azure PAT is not configured in Settings.");
        }

        var header = BuildAuthHeader(pat);
        await RunGitAsync(
            repoPath,
            $" -c http.extraheader=\"AUTHORIZATION: basic {header}\" push -u origin \"{EscapeArg(item.BranchName!)}\"");

        _logger.LogInformation("Pushed branch {BranchName} for WorkItem {WorkItemId}", item.BranchName, item.Id);
        return $"Pushed {item.BranchName} to origin.";
    }

    private async Task<(Project Project, string RepoPath)> ResolveRepoAsync(WorkItem item)
    {
        var project = await _projectService.GetByIdAsync(item.ProjectId)
            ?? throw new InvalidOperationException($"Project {item.ProjectId} was not found.");

        var repoPath = Path.GetFullPath(project.LocalRepoPath);
        if (!Directory.Exists(repoPath))
        {
            throw new InvalidOperationException($"Repository path does not exist: {repoPath}");
        }

        return (project, repoPath);
    }

    private static void EnsureBranchName(WorkItem item)
    {
        if (string.IsNullOrWhiteSpace(item.BranchName))
        {
            throw new InvalidOperationException("Work item does not have a branch yet. Move it to In Progress first.");
        }
    }

    private static async Task EnsureOnBranchAsync(string repoPath, string branchName)
    {
        var current = await RunGitAsync(repoPath, "rev-parse --abbrev-ref HEAD");
        if (!string.Equals(current, branchName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Current branch is '{current}'. Switch to '{branchName}' first.");
        }
    }

    private static async Task EnsureOriginRemoteAsync(string repoPath, string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            throw new InvalidOperationException("Project remote URL is empty.");
        }

        string existing;
        try
        {
            existing = await RunGitAsync(repoPath, "remote get-url origin");
        }
        catch
        {
            await RunGitAsync(repoPath, $"remote add origin \"{EscapeArg(remoteUrl.Trim())}\"");
            return;
        }

        if (!string.Equals(existing.Trim(), remoteUrl.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await RunGitAsync(repoPath, $"remote set-url origin \"{EscapeArg(remoteUrl.Trim())}\"");
        }
    }

    private static string BuildAuthHeader(string pat)
    {
        var bytes = Encoding.ASCII.GetBytes($":{pat.Trim()}");
        return Convert.ToBase64String(bytes);
    }

    private static string EscapeArg(string value) => value.Replace("\"", "\\\"");

    private static async Task<string> RunGitAsync(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments.TrimStart(),
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Git command failed ({arguments}) in '{workingDirectory}'. {error}");
        }

        return output;
    }
}
