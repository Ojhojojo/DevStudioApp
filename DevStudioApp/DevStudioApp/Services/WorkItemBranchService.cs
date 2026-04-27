using System.Diagnostics;
using System.Text;
using DevStudioDomain.Entities;
using Microsoft.Extensions.Logging;

namespace DevStudioApp.Services;

public interface IWorkItemBranchService
{
    Task<BranchProvisionResult> ProvisionForInProgressAsync(WorkItem item);
}

public sealed record BranchProvisionResult(
    string BranchName,
    string BaseBranch,
    bool RemoteCheckSucceeded);

public class WorkItemBranchService(IProjectService projectService, ILogger<WorkItemBranchService> logger) : IWorkItemBranchService
{
    private readonly IProjectService _projectService = projectService;
    private readonly ILogger<WorkItemBranchService> _logger = logger;

    public async Task<BranchProvisionResult> ProvisionForInProgressAsync(WorkItem item)
    {
        var project = await _projectService.GetByIdAsync(item.ProjectId)
            ?? throw new InvalidOperationException($"Project {item.ProjectId} was not found.");

        if (string.IsNullOrWhiteSpace(project.LocalRepoPath))
        {
            throw new InvalidOperationException("Project local repository path is missing.");
        }

        var repoPath = Path.GetFullPath(project.LocalRepoPath);
        if (!Directory.Exists(repoPath))
        {
            throw new InvalidOperationException($"Repository path does not exist: {repoPath}");
        }

        var baseBranch = await RunGitAsync(repoPath, "rev-parse --abbrev-ref HEAD");
        if (string.Equals(baseBranch, "HEAD", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot create task branch from detached HEAD.");
        }

        var slug = Slugify(item.Title);
        var rootName = $"feature/{item.Id}-{slug}";
        var remoteCheckSucceeded = true;
        var suffix = 1;
        string branchName;
        while (true)
        {
            branchName = suffix == 1 ? rootName : $"{rootName}-{suffix}";
            var existsLocal = await LocalBranchExistsAsync(repoPath, branchName);
            var existsRemote = false;
            try
            {
                existsRemote = await RemoteBranchExistsAsync(repoPath, branchName);
            }
            catch (Exception ex)
            {
                remoteCheckSucceeded = false;
                _logger.LogWarning(ex, "Remote branch check failed for {BranchName}. Falling back to local-only conflict checks.", branchName);
            }

            if (!existsLocal && !existsRemote)
            {
                break;
            }

            suffix++;
        }

        await RunGitAsync(repoPath, $"checkout -b \"{branchName}\"");

        _logger.LogInformation(
            "Created task branch {BranchName} from {BaseBranch} for WorkItem {WorkItemId}",
            branchName,
            baseBranch,
            item.Id);

        return new BranchProvisionResult(branchName, baseBranch, remoteCheckSucceeded);
    }

    private static string Slugify(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "work-item";
        }

        var sb = new StringBuilder();
        var lastWasDash = false;
        foreach (var ch in title.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastWasDash = false;
                continue;
            }

            if (lastWasDash)
            {
                continue;
            }

            sb.Append('-');
            lastWasDash = true;
        }

        var slug = sb.ToString().Trim('-');
        if (slug.Length == 0)
        {
            return "work-item";
        }

        return slug.Length <= 48 ? slug : slug[..48].TrimEnd('-');
    }

    private async Task<bool> LocalBranchExistsAsync(string repoPath, string branchName)
    {
        var output = await RunGitAsync(repoPath, $"branch --list \"{branchName}\"");
        return !string.IsNullOrWhiteSpace(output);
    }

    private async Task<bool> RemoteBranchExistsAsync(string repoPath, string branchName)
    {
        var output = await RunGitAsync(repoPath, $"ls-remote --heads origin \"{branchName}\"");
        return !string.IsNullOrWhiteSpace(output);
    }

    private static async Task<string> RunGitAsync(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
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
