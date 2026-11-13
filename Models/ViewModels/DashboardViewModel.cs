
namespace EmployeeManagement.Web.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalEmployees { get; set; }

    public int ActiveEmployees { get; set; }

    public int InactiveEmployees { get; set; }

    public int TotalDepartments { get; set; }

    public List<DepartmentEmployeeStat> EmployeesByDepartment { get; set; }
        = new();

    public List<Employee> RecentEmployees { get; set; }
        = new();
}

public class DepartmentEmployeeStat
{
    public string DepartmentName { get; set; } = string.Empty;

    public int EmployeeCount { get; set; }
}
