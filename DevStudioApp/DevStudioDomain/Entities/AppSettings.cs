using System.ComponentModel.DataAnnotations;

namespace DevStudioDomain.Entities;

public class AppSettings
{
    [Key]
    public int Id { get; set; } = 1;

    [Required]
    [MaxLength(20)]
    public string Theme { get; set; } = "light";

    [Required]
    [MaxLength(20)]
    public string LoggingLevel { get; set; } = "Information";

    [Required]
    [MaxLength(500)]
    public string DefaultWorktreeBasePath { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
