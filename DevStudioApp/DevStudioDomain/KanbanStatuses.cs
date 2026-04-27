namespace DevStudioDomain;

public static class KanbanStatuses
{
    public const string Backlog = "backlog";
    public const string Planned = "planned";
    public const string InProgress = "in-progress";
    public const string Review = "review";
    public const string Done = "done";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Backlog,
        Planned,
        InProgress,
        Review,
        Done
    };

    public static string DisplayLabel(string status) => status switch
    {
        Backlog => "Backlog",
        Planned => "Planned",
        InProgress => "In Progress",
        Review => "Review",
        Done => "Done",
        _ => status
    };
}
