namespace MedRemind.Web.Models;

public class ApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public int Timeout { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
}

public class AppSettings
{
    public string AppName { get; set; } = "MedRemind";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
}
