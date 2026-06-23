namespace LibWiseApp.ViewModels;

public class DashboardViewModel
{
    public int TotalBooks { get; set; }
    public int AvailableBooks { get; set; }
    public int BorrowedBooks { get; set; }
    public int OverdueBooks { get; set; }
    public int TotalBorrowers { get; set; }
    public int ActiveBorrowings { get; set; }
    public int UnpaidFines { get; set; }
    public decimal TotalFinesCollected { get; set; }
    public List<RecentActivity> RecentActivities { get; set; } = new();
}

public class RecentActivity
{
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
