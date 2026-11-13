namespace EmployeeManagement.Web.Models.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsLocked { get; set; }
}