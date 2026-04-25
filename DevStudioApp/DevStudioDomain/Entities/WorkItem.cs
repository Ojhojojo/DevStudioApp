using System.ComponentModel.DataAnnotations;

namespace DevStudioDomain.Entities;

public class WorkItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "backlog";
    // Status values: backlog, planning, executing, waiting-for-review, in-review, done

    [MaxLength(10000)]
    public string? Plan { get; set; }

    // Git/PR tracking
    [MaxLength(200)]
    public string? BranchName { get; set; }

    [MaxLength(500)]
    public string? PullRequestUrl { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}