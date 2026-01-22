using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI.Interfaces;

/// <summary>
/// Base interface for all AI prescription parsers
/// Enables dynamic parser registration and strategy pattern
/// </summary>
public interface IPrescriptionParser
{
    /// <summary>
    /// Parser name for identification and logging
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Execution priority (1 = highest priority, execute first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Whether this parser is currently enabled
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Maximum time allowed for this parser to execute
    /// </summary>
    TimeSpan Timeout { get; }
    
    /// <summary>
    /// Parse OCR text into structured prescription data
    /// </summary>
    Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Health check to verify parser is operational
    /// </summary>
    Task<bool> HealthCheckAsync();
}
