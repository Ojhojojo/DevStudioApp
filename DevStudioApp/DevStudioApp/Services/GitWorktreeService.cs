//using System.Diagnostics;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using DevStudioDomain.Entities;

//<<<<<<< TODO: Unmerged change from project 'DevStudioApp (net8.0-windows10.0.19041.0)', Before:
//=======
//using DevForge;
//using DevForge.AgenticStudio;
//using DevForge.AgenticStudio.Desktop;
//using DevForge.AgenticStudio.Desktop.Services;
//using DevStudioApp.Services;
//>>>>>>> After

//namespace DevStudioApp.Services;

//public interface IGitWorktreeService
//{
//    Task<string> CreateWorktreeForItemAsync(int kanbanItemId, string featureName, string projectRootPath);
//    Task<string> ApplyCodeToWorktreeAsync(int kanbanItemId, string filePath, string content);
//    Task<string> CommitWorktreeAsync(string worktreePath, string commitMessage);
//    Task<string> CleanupWorktreeAsync(int kanbanItemId);
//    Task<string> RunCopilotAutopilotAsync(int kanbanItemId, string worktreePath, string prompt);
//    Task<string> GetCurrentBranchAsync(string worktreePath);
//    Task<string> PushBranchAsync(string worktreePath, string branchName);
//    Task<string> CreatePullRequestAsync(Project project, string worktreePath, string prTitle, string prDescription);
//}

//public class GitWorktreeService(IConfiguration configuration, ILogger<GitWorktreeService> logger) : IGitWorktreeService
//{
//    private readonly ILogger<GitWorktreeService> _logger = logger;

//    private string GetCopilotLogPath(string worktreePath)
//    {
//        // For desktop, use AppData for logs instead of web ContentRootPath
//        var logsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevForgeAgenticStudio", "Logs", "copilot");
//        Directory.CreateDirectory(logsDirectory);

//        var worktreeName = Path.GetFileName(worktreePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
//        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
//        return Path.Combine(logsDirectory, $"{timestamp}-{worktreeName}.log");
//    }

//    private static string? ResolveCopilotCommandPath()
//    {
//        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

//        var candidates = new[]
//        {
//            Path.Combine(appData, "Code", "User", "globalStorage", "github.copilot-chat", "copilotCli", "copilot.bat"),
//            Path.Combine(appData, "npm", "copilot.cmd"),
//            Path.Combine(appData, "npm", "copilot")
//        };

//        return candidates.FirstOrDefault(File.Exists);
//    }

//    public async Task<string> CreateWorktreeForItemAsync(int kanbanItemId, string featureName, string projectRootPath)
//    {
//        var worktreeName = $"feature-{kanbanItemId}-{featureName.Replace(" ", "-").ToLowerInvariant()}";
//        var worktreesDir = Path.Combine(projectRootPath, ".worktrees");
//        Directory.CreateDirectory(worktreesDir);

//        var worktreePath = Path.Combine(worktreesDir, worktreeName);

//        _logger.LogInformation("Creating worktree {WorktreeName} for project path {ProjectRootPath}", worktreeName, projectRootPath);

//        // Remove stale directory from a previous failed run
//        if (Directory.Exists(worktreePath))
//        {
//            _logger.LogWarning("Removing stale worktree directory {WorktreePath}", worktreePath);
//            Directory.Delete(worktreePath, recursive: true);
//        }

//        // Prune any stale worktree metadata git may still hold for this path
//        _logger.LogDebug("Pruning stale worktree metadata in {ProjectRootPath}", projectRootPath);
//        await RunGitCommandAsync("worktree prune", projectRootPath);
//        await Task.Delay(500); // small safety delay

//        // Delete the branch if it already exists so we can recreate it cleanly
//        try
//        {
//            _logger.LogDebug("Deleting existing git branch {WorktreeName} if present", worktreeName);
//            await RunGitCommandAsync($"branch -D {worktreeName}", projectRootPath);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogDebug(ex, "Branch {WorktreeName} did not exist or could not be deleted", worktreeName);
//        }

//        // Check if the repository is in an unborn HEAD state (empty repo with no commits)
//        var isUnborn = await IsRepositoryUnbornAsync(projectRootPath);
//        _logger.LogInformation("Repository unborn state for {ProjectRootPath}: {IsUnborn}", projectRootPath, isUnborn);

//        if (isUnborn)
//        {
//            _logger.LogInformation("Creating orphan git worktree for branch {WorktreeName}", worktreeName);
//            await RunGitCommandAsync($"worktree add --orphan -b {worktreeName} \"{worktreePath}\"", projectRootPath);
//            await InitializeOrphanedWorktreeAsync(worktreePath, worktreeName);
//        }
//        else
//        {
//            _logger.LogInformation("Creating standard git worktree for branch {WorktreeName}", worktreeName);
//            await RunGitCommandAsync($"worktree add -f -b {worktreeName} \"{worktreePath}\"", projectRootPath);
//        }

//        _logger.LogInformation("Worktree created at {WorktreePath}", worktreePath);
//        return worktreePath;
//    }

//    public async Task<string> ApplyCodeToWorktreeAsync(int kanbanItemId, string filePath, string content)
//    {
//        // TODO: Later we'll get the correct worktree path from DB
//        // For now we simulate
//        await File.WriteAllTextAsync(filePath, content);
//        return $"Created/updated {filePath}";
//    }

//    public async Task<string> CommitWorktreeAsync(string worktreePath, string commitMessage)
//    {
//        if (!Directory.Exists(worktreePath))
//            throw new DirectoryNotFoundException($"Worktree path not found: {worktreePath}");

//        await RunGitCommandAsync("add .", worktreePath);

//        var status = await RunGitCommandAsync("status --short", worktreePath);
//        if (string.IsNullOrWhiteSpace(status))
//            return "No changes to commit";

//        await RunGitCommandAsync($"commit -m \"{commitMessage}\"", worktreePath);
//        return "Changes committed in worktree";
//    }

//    public Task<string> CleanupWorktreeAsync(int kanbanItemId)
//    {
//        // Later: remove worktree
//        return Task.FromResult("Worktree cleaned up");
//    }

//    private async Task<bool> IsRepositoryUnbornAsync(string workingDirectory)
//    {
//        var process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = "git",
//                Arguments = "rev-parse --verify --quiet HEAD",
//                WorkingDirectory = workingDirectory,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                UseShellExecute = false
//            }
//        };

//        process.Start();
//        await process.WaitForExitAsync();

//        return process.ExitCode != 0;
//    }

//    private async Task InitializeOrphanedWorktreeAsync(string worktreePath, string branchName)
//    {
//        // Create a .gitkeep file to ensure the worktree has at least one file
//        var gitkeepPath = Path.Combine(worktreePath, ".gitkeep");
//        await File.WriteAllTextAsync(gitkeepPath, string.Empty);

//        // Stage and commit the .gitkeep file
//        await RunGitCommandAsync("add .gitkeep", worktreePath);
//        await RunGitCommandAsync($"commit -m \"Initialize branch {branchName}\"", worktreePath);
//    }

//    private async Task<string> RunGitCommandAsync(string arguments, string workingDirectory)
//    {
//        var process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = "git",
//                Arguments = arguments,
//                WorkingDirectory = workingDirectory,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                UseShellExecute = false
//            }
//        };

//        process.Start();

//        var outputTask = process.StandardOutput.ReadToEndAsync();
//        var errorTask = process.StandardError.ReadToEndAsync();

//        await process.WaitForExitAsync();

//        var output = await outputTask;
//        var error = await errorTask;

//        if (process.ExitCode != 0)
//        {
//            _logger.LogError(
//                "Git command failed: git {Arguments} in {WorkingDirectory}. ExitCode={ExitCode}. Error={Error}. Output={Output}",
//                arguments,
//                workingDirectory,
//                process.ExitCode,
//                error,
//                output);

//            throw new Exception($"Git command failed for 'git {arguments}' in '{workingDirectory}'.\nError: {error}\nOutput: {output}");
//        }

//        return output;
//    }

//    public async Task<string> RunCopilotAutopilotAsync(int kanbanItemId, string worktreePath, string taskPrompt)
//    {
//        if (!Directory.Exists(worktreePath))
//            throw new DirectoryNotFoundException($"Worktree path not found: {worktreePath}");

//        var logPath = GetCopilotLogPath(worktreePath);
//        var tasksDirectory = Path.Combine(worktreePath, ".devforge", "tasks");
//        Directory.CreateDirectory(tasksDirectory);

//        var instructionFileName = $"task-{kanbanItemId}.md";
//        var instructionPath = Path.Combine(tasksDirectory, instructionFileName);
//        await File.WriteAllTextAsync(instructionPath, taskPrompt);

//        var cliPrompt =
//            $"Read .devforge/tasks/{instructionFileName} from the current repository and implement the requested changes. " +
//            "Apply edits directly in this repo and finish by saying TASK COMPLETE.";

//        var promptPath = Path.ChangeExtension(logPath, ".prompt.txt");
//        await File.WriteAllTextAsync(promptPath, cliPrompt);
//        var copilotCommandPath = ResolveCopilotCommandPath();

//        // For desktop, look for script in AppData or relative to executable
//        var scriptCandidates = new[]
//        {
//            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevForgeAgenticStudio", "Scripts", "run-copilot-autopilot.ps1"),
//            Path.Combine(AppContext.BaseDirectory, "Scripts", "run-copilot-autopilot.ps1"),
//            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Scripts", "run-copilot-autopilot.ps1"))
//        };

//        var scriptPath = scriptCandidates.FirstOrDefault(File.Exists);
//        if (scriptPath == null)
//            throw new FileNotFoundException($"Copilot runner script not found. Checked: {string.Join(" | ", scriptCandidates)}");

//        var process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = "powershell.exe",
//                WorkingDirectory = worktreePath,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                UseShellExecute = false,
//                CreateNoWindow = true
//            }
//        };

//        process.StartInfo.ArgumentList.Add("-NoLogo");
//        process.StartInfo.ArgumentList.Add("-NoProfile");
//        process.StartInfo.ArgumentList.Add("-NonInteractive");
//        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
//        process.StartInfo.ArgumentList.Add("Bypass");
//        process.StartInfo.ArgumentList.Add("-File");
//        process.StartInfo.ArgumentList.Add(scriptPath);
//        process.StartInfo.ArgumentList.Add("-WorktreePath");
//        process.StartInfo.ArgumentList.Add(worktreePath);
//        process.StartInfo.ArgumentList.Add("-TaskPromptFile");
//        process.StartInfo.ArgumentList.Add(promptPath);
//        process.StartInfo.ArgumentList.Add("-CopilotCommandPath");
//        process.StartInfo.ArgumentList.Add(copilotCommandPath ?? string.Empty);
//        process.StartInfo.ArgumentList.Add("-MaxAutopilotContinues");
//        process.StartInfo.ArgumentList.Add("10");

//        process.Start();

//        var outputTask = process.StandardOutput.ReadToEndAsync();
//        var errorTask = process.StandardError.ReadToEndAsync();

//        await process.WaitForExitAsync();

//        var output = await outputTask;
//        var error = await errorTask;

//        var statusAfterRun = await RunGitCommandAsync("status --short", worktreePath);

//        var logContent = string.Join(Environment.NewLine, new[]
//        {
//            $"TimestampUtc: {DateTime.UtcNow:O}",
//            $"WorktreePath: {worktreePath}",
//            $"ScriptPath: {scriptPath}",
//            $"PromptPath: {promptPath}",
//            $"InstructionPath: {instructionPath}",
//            $"ResolvedCopilotCommandPath: {copilotCommandPath}",
//            $"ExitCode: {process.ExitCode}",
//            "GitStatusAfterRun:",
//            statusAfterRun,
//            "Prompt:",
//            cliPrompt,
//            string.Empty,
//            "FullTaskPrompt:",
//            taskPrompt,
//            string.Empty,
//            "StdOut:",
//            output,
//            string.Empty,
//            "StdErr:",
//            error
//        });

//        await File.WriteAllTextAsync(logPath, logContent);
//        _logger.LogInformation("Execution log saved to {LogPath}", logPath);

//        if (process.ExitCode != 0)
//        {
//            throw new Exception($"Copilot autopilot failed (exit code {process.ExitCode}) using script '{scriptPath}'. Log: {logPath}\nError: {error}\nOutput: {output}");
//        }

//        if (string.IsNullOrWhiteSpace(statusAfterRun))
//        {
//            throw new Exception($"Copilot exited successfully but produced no file changes. Log: {logPath}");
//        }

//        return $"Log saved to: {logPath}{Environment.NewLine}{output}";
//    }

//    public async Task<string> GetCurrentBranchAsync(string worktreePath)
//    {
//        var branchName = await RunGitCommandWithOutputAsync("branch --show-current", worktreePath);
//        _logger.LogDebug("Current branch in {WorktreePath}: {BranchName}", worktreePath, branchName);
//        return branchName;
//    }

//    public async Task<string> PushBranchAsync(string worktreePath, string branchName)
//    {
//        _logger.LogInformation("Pushing branch {BranchName} to origin from {WorktreePath}", branchName, worktreePath);
//        await RunGitCommandAsync($"push -u origin {branchName}", worktreePath);
//        return $"Branch {branchName} pushed to origin";
//    }

//    public async Task<string> CreatePullRequestAsync(Project project, string worktreePath, string prTitle, string prDescription)
//    {
//        var pat = configuration["AzureDevOps:Pat"];
//        var org = project.AzureDevOpsOrg ?? configuration["AzureDevOps:Organization"];
//        var projectName = project.AzureDevOpsProject ?? project.Name;
//        var repositoryName = ExtractRepoNameFromRemote(project.RemoteUrl) ?? project.AzureDevOpsProject ?? project.Name;

//        if (string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(org) || string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(repositoryName))
//        {
//            return "Azure DevOps PAT, Organization, Project or Repository name not configured.";
//        }

//        try
//        {
//            // Get current branch
//            var branchName = await RunGitCommandWithOutputAsync("branch --show-current", worktreePath);
//            if (string.IsNullOrWhiteSpace(branchName))
//                throw new Exception("Could not determine current git branch.");

//            _logger.LogInformation("Creating PR for branch {BranchName} in repository {RepositoryName}", branchName, repositoryName);

//            // Determine target branch from remote (main/master fallback)
//            var targetBranch = await ResolveTargetBranchAsync(worktreePath);
//            _logger.LogInformation("Resolved PR target branch {TargetBranch}", targetBranch);

//            // Azure DevOps REST API - Create PR
//            var url = $"https://dev.azure.com/{org}/{projectName}/_apis/git/repositories/{repositoryName}/pullrequests?api-version=7.1";
//            _logger.LogInformation("Creating Azure DevOps PR at {Url}", url);

//            var prBody = new
//            {
//                sourceRefName = $"refs/heads/{branchName}",
//                targetRefName = $"refs/heads/{targetBranch}",
//                title = prTitle,
//                description = prDescription
//            };

//            using var client = new HttpClient();
//            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
//            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

//            var content = new StringContent(JsonSerializer.Serialize(prBody), Encoding.UTF8, "application/json");

//            var response = await client.PostAsync(url, content);

//            if (response.IsSuccessStatusCode)
//            {
//                var responseContent = await response.Content.ReadAsStringAsync();
//                _logger.LogInformation("Pull request successfully created for branch {BranchName} targeting {TargetBranch}. Response: {Response}", branchName, targetBranch, responseContent);
//                return $"✅ Pull Request created successfully in Azure DevOps for branch '{branchName}' targeting '{targetBranch}'";
//            }
//            else
//            {
//                var errorBody = await response.Content.ReadAsStringAsync();
//                _logger.LogError("Azure DevOps API error creating PR: {StatusCode} - {ErrorBody}", response.StatusCode, errorBody);
//                throw new Exception($"Azure DevOps API error: {response.StatusCode} - {errorBody}");
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to create pull request for repository {RepositoryName}", repositoryName);
//            throw;
//        }
//    }

//    private static string? ExtractRepoNameFromRemote(string? remoteUrl)
//    {
//        if (string.IsNullOrWhiteSpace(remoteUrl))
//            return null;

//        // Azure DevOps remote urls are typically: https://dev.azure.com/{org}/{project}/_git/{repo}
//        var trimmed = remoteUrl.Trim();
//        if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
//            trimmed = trimmed[..^4];

//        if (trimmed.Contains("/_git/", StringComparison.OrdinalIgnoreCase))
//        {
//            return trimmed.Split(new[] { "/_git/" }, StringSplitOptions.None).Last();
//        }

//        // Fallback to last path segment for generic remotes
//        var separator = trimmed.Contains('/') ? '/' : ':';
//        var lastSegment = trimmed.Split(separator).LastOrDefault();
//        return string.IsNullOrWhiteSpace(lastSegment) ? null : lastSegment;
//    }

//    private async Task<string> ResolveTargetBranchAsync(string workingDirectory)
//    {
//        var candidates = new[] { "main", "master" };
//        foreach (var branch in candidates)
//        {
//            try
//            {
//                var output = await RunGitCommandWithOutputAsync($"ls-remote --heads origin {branch}", workingDirectory);
//                if (!string.IsNullOrWhiteSpace(output))
//                    return branch;
//            }
//            catch
//            {
//                // ignore and continue to next candidate
//            }
//        }

//        return "main";
//    }

//    private async Task<string> RunGitCommandWithOutputAsync(string arguments, string workingDirectory)
//    {
//        var process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = "git",
//                Arguments = arguments,
//                WorkingDirectory = workingDirectory,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                UseShellExecute = false
//            }
//        };

//        process.Start();
//        var outputTask = process.StandardOutput.ReadToEndAsync();
//        var errorTask = process.StandardError.ReadToEndAsync();

//        await process.WaitForExitAsync();

//        var output = (await outputTask).Trim();
//        var error = await errorTask;

//        if (process.ExitCode != 0)
//        {
//            _logger.LogWarning(
//                "Git command returned non-zero exit code: git {Arguments} in {WorkingDirectory}. ExitCode={ExitCode}. Error={Error}. Output={Output}",
//                arguments,
//                workingDirectory,
//                process.ExitCode,
//                error,
//                output);
//        }

//        return output;
//    }
//}