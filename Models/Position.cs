
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Web.Models;

public class Position
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Position name is required.")]
    [StringLength(100)]
    [Display(Name = "Position Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }
}
