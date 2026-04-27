using System.ComponentModel.DataAnnotations;

namespace DevStudioDomain.Entities;

public class WorkItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(10000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = KanbanStatuses.Backlog;

    public int Order { get; set; }

    [MaxLength(10000)]
    public string? Plan { get; set; }

    [MaxLength(200)]
    public string? BranchName { get; set; }

    [MaxLength(500)]
    public string? PullRequestUrl { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
