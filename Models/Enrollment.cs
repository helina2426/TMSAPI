using System.ComponentModel.DataAnnotations;

namespace TmsApi.Models;

public class Enrollment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public string CourseCode { get; set; } = string.Empty;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}