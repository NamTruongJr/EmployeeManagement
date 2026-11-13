using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Web.Models.ViewModels;

public class EditUserRoleViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string? Email { get; set; }

    [Required(ErrorMessage = "Please select a role.")]
    public string Role { get; set; } = "HR";
}