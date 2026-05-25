namespace EmployeeDirectory.ViewModels;

/// <summary>
/// Supplies dashboard-level analytics and recent activity content for the admin home page.
/// </summary>
public class AdminDashboardViewModel
{
    public int TotalEmployees { get; set; }

    public int TotalDepartments { get; set; }

    public int NewHiresThisMonth { get; set; }

    public int InactiveEmployees { get; set; }

    public int EmployeeTrendDelta { get; set; }

    public string EmployeeTrendDirection { get; set; } = "up";

    public List<DepartmentSummaryViewModel> EmployeesPerDepartment { get; set; } = new();

    public Dictionary<string, int> RoleDistribution { get; set; } = new();

    public List<HiresSeriesPointViewModel> NewHireSeries { get; set; } = new();

    public List<RecentActivityViewModel> RecentActivity { get; set; } = new();
}

/// <summary>
/// Represents a department with employee count metrics for charting.
/// </summary>
public class DepartmentSummaryViewModel
{
    public string DepartmentName { get; set; } = string.Empty;

    public int EmployeeCount { get; set; }
}

/// <summary>
/// Represents a single month data point for the new hire trend line.
/// </summary>
public class HiresSeriesPointViewModel
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}

/// <summary>
/// Represents one row in the dashboard recent activity feed.
/// </summary>
public class RecentActivityViewModel
{
    public string ActionType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string? ActingUserEmail { get; set; }
}
