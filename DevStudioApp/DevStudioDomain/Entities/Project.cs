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
    public required string LocalPath { get; set; }

    [Required]
    [MaxLength(500)]
    public required string RemoteUrl { get; set; }

    [Required]
    [MaxLength(500)]
    public required string AzureDevOpsProject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}