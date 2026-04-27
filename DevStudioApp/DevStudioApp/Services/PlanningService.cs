using System.Diagnostics;
using System.Text;
using DevStudioDomain.Entities;
using Microsoft.Extensions.Logging;

namespace DevStudioApp.Services;

public sealed record PlanningResult(
    bool Succeeded,
    string ContextSummary,
    string GeneratedPlan,
    string Diagnostics);

public interface IPlanningService
{
    Task<PlanningResult> GeneratePlanAsync(WorkItem item, CancellationToken cancellationToken = default);
}

public class PlanningService(
    IProjectService projectService,
    ITokenService tokenService,
    ILogger<PlanningService> logger) : IPlanningService
{
    private readonly IProjectService _projectService = projectService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<PlanningService> _logger = logger;

    public async Task<PlanningResult> GeneratePlanAsync(WorkItem item, CancellationToken cancellationToken = default)
    {
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

        var context = await BuildContextSummaryAsync(repoPath, item);
        var prompt = BuildPlanningPrompt(project, item, context);
        var (exitCode, output) = await RunCopilotAsync(repoPath, token, prompt, cancellationToken);
        var success = exitCode == 0 && !string.IsNullOrWhiteSpace(output);

        _logger.LogInformation(
            "Generated plan for WorkItem {WorkItemId} (success={Success}, exitCode={ExitCode})",
            item.Id,
            success,
            exitCode);

        return new PlanningResult(
            success,
            context,
            output.Trim(),
            $"copilot exit code: {exitCode}");
    }

    private static async Task<string> BuildContextSummaryAsync(string repoPath, WorkItem item)
    {
        var sb = new StringBuilder();
        var branch = await RunGitSafeAsync(repoPath, "rev-parse --abbrev-ref HEAD");
        var status = await RunGitSafeAsync(repoPath, "status --short");

        sb.AppendLine($"CurrentBranch: {branch}");
        sb.AppendLine("ChangedFiles:");
        if (string.IsNullOrWhiteSpace(status))
        {
            sb.AppendLine("- (none)");
        }
        else
        {
            foreach (var line in status.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Take(30))
            {
                sb.AppendLine($"- {line}");
            }
        }

        sb.AppendLine("TopLevelProjectFiles:");
        var topFiles = Directory.EnumerateFiles(repoPath)
            .Where(path =>
            {
                var file = Path.GetFileName(path);
                return file.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                       file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                       file.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                       file.Equals("readme.md", StringComparison.OrdinalIgnoreCase);
            })
            .Take(10)
            .Select(Path.GetFileName);

        foreach (var file in topFiles)
        {
            sb.AppendLine($"- {file}");
        }

        sb.AppendLine("WorkItem:");
        sb.AppendLine($"- Id: {item.Id}");
        sb.AppendLine($"- Title: {item.Title}");
        sb.AppendLine($"- Status: {item.Status}");
        sb.AppendLine($"- Branch: {item.BranchName ?? "(none)"}");
        sb.AppendLine($"- Description: {TrimLength(item.Description, 1200)}");

        return TrimLength(sb.ToString().Trim(), 5000);
    }

    private static string BuildPlanningPrompt(Project project, WorkItem item, string context)
    {
        return $"""
You are planning implementation work for a .NET MAUI Blazor Hybrid app.
Return markdown only.

Project: {project.Name}
WorkItemId: {item.Id}
WorkItemTitle: {item.Title}
WorkItemStatus: {item.Status}
WorkItemBranch: {item.BranchName ?? "(none)"}

Repository context:
{context}

Task description:
{item.Description}

Produce a concise implementation plan with:
1) Goal
2) 5-8 concrete implementation steps
3) Key files likely to change
4) Test/validation checklist
""";
    }

    private static async Task EnsureCopilotCliAvailableAsync(CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "copilot",
            Arguments = "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Copilot CLI is not available. Ensure `copilot` is installed and on PATH.");
        }
    }

    private static async Task<(int ExitCode, string Output)> RunCopilotAsync(
        string repoPath,
        string token,
        string prompt,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "copilot",
            Arguments = $"\"{prompt.Replace("\"", "\\\"")}\"",
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["COPILOT_TOKEN"] = token.Trim();
        psi.Environment["GITHUB_TOKEN"] = token.Trim();

        using var process = new Process { StartInfo = psi };
        process.Start();
        var outTask = process.StandardOutput.ReadToEndAsync();
        var errTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await outTask;
        var stderr = await errTask;
        var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
        return (process.ExitCode, output);
    }

    private static async Task<string> RunGitSafeAsync(string workingDirectory, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = psi };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output.Trim();
        }
        catch
        {
            return "(unavailable)";
        }
    }

    private static string TrimLength(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        return value.Length <= maxLen ? value : value[..maxLen] + "...";
    }
}
