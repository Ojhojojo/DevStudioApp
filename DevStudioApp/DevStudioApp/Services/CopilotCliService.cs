using System.Diagnostics;
using System.Text;
using DevStudioDomain.Entities;
using Microsoft.Extensions.Logging;

namespace DevStudioApp.Services;

public sealed record CopilotRunResult(bool Succeeded, int ExitCode, string Output);

public interface ICopilotCliService
{
    Task<CopilotRunResult> RunForWorkItemAsync(
        WorkItem item,
        string userPrompt,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default);
}

public class CopilotCliService(
    IProjectService projectService,
    ITokenService tokenService,
    ILogger<CopilotCliService> logger) : ICopilotCliService
{
    private readonly IProjectService _projectService = projectService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<CopilotCliService> _logger = logger;

    public async Task<CopilotRunResult> RunForWorkItemAsync(
        WorkItem item,
        string userPrompt,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new InvalidOperationException("Copilot prompt is required.");
        }

        var project = await _projectService.GetByIdAsync(item.ProjectId)
            ?? throw new InvalidOperationException($"Project {item.ProjectId} was not found.");
        var repoPath = Path.GetFullPath(project.LocalRepoPath);
        if (!Directory.Exists(repoPath))
        {
            throw new InvalidOperationException($"Repository path does not exist: {repoPath}");
        }

        var token = await _tokenService.GetCopilotTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Copilot token is not configured in Settings.");
        }

        await EnsureCopilotCliAvailableAsync(cancellationToken);

        var prompt = BuildPrompt(project, item, userPrompt);
        var outputBuilder = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = "copilot",
            Arguments = $"\"{prompt.Replace("\"", "\\\"")}\"",
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["COPILOT_TOKEN"] = token.Trim();
        startInfo.Environment["GITHUB_TOKEN"] = token.Trim();

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            outputBuilder.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            outputBuilder.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        var output = outputBuilder.ToString().Trim();
        var succeeded = process.ExitCode == 0;
        _logger.LogInformation(
            "Copilot run for WorkItem {WorkItemId} in project {ProjectId} finished with exit code {ExitCode}",
            item.Id,
            item.ProjectId,
            process.ExitCode);

        return new CopilotRunResult(succeeded, process.ExitCode, output);
    }

    private static async Task EnsureCopilotCliAvailableAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "copilot",
            Arguments = "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Copilot CLI is not available. Ensure `copilot` is installed and on PATH.");
        }
    }

    private static string BuildPrompt(Project project, WorkItem item, string userPrompt)
    {
        return $"""
You are working on a task in Dev Agent Studio.

Project: {project.Name}
Repository Path: {project.LocalRepoPath}
Work Item Id: {item.Id}
Work Item Title: {item.Title}
Work Item Status: {item.Status}
Branch Name: {item.BranchName ?? "(none)"}
Task Description:
{item.Description}

User instruction:
{userPrompt}

Apply code changes in this repository only.
""";
    }
}
