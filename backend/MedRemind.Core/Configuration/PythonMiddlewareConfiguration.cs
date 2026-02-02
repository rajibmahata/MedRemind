namespace MedRemind.Core.Configuration;

/// <summary>
/// Configuration for Python Middleware (CrewAI) client
/// </summary>
public class PythonMiddlewareConfiguration
{
    /// <summary>
    /// Base URL of the Python middleware service
    /// </summary>
    public string BaseApiUrl { get; set; } = "http://localhost:8000";

    /// <summary>
    /// Whether Python middleware is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes default for AI processing
}
