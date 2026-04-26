using System.ComponentModel.DataAnnotations;

namespace DevStudioDomain.Entities;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public required string LocalRepoPath { get; set; }

    [Required]
    [MaxLength(500)]
    public required string RemoteAzureRepoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}