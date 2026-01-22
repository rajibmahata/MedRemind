using MedRemind.Core.DTOs;
using MedRemind.Services.AI.Interfaces;

namespace MedRemind.Services.AI.Adapters;

/// <summary>
/// Adapter for DeepSeekPrescriptionParserAgent to implement IPrescriptionParser
/// </summary>
public class DeepSeekParserAdapter : IPrescriptionParser
{
    private readonly DeepSeekPrescriptionParserAgent _deepSeekParser;
    private readonly bool _isEnabled;
    private readonly int _priority;
    
    public string Name => "DeepSeek";
    public int Priority => _priority;
    public bool IsEnabled => _isEnabled;
    public TimeSpan Timeout => TimeSpan.FromSeconds(30); // Increased from 20s - DeepSeek is slower
    
    public DeepSeekParserAdapter(
        DeepSeekPrescriptionParserAgent deepSeekParser,
        bool isEnabled,
        int priority)
    {
        _deepSeekParser = deepSeekParser ?? throw new ArgumentNullException(nameof(deepSeekParser));
        _isEnabled = isEnabled;
        _priority = priority;
    }
    
    public Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        return _deepSeekParser.ParsePrescriptionTextAsync(ocrText, cancellationToken);
    }
    
    public Task<bool> HealthCheckAsync()
    {
        // Basic health check - just verify parser is not null
        return Task.FromResult(_deepSeekParser != null);
    }
}

/// <summary>
/// Adapter for OpenAIParserAgent to implement IPrescriptionParser
/// </summary>
public class OpenAIParserAdapter : IPrescriptionParser
{
    private readonly OpenAIPrescriptionParserAgent _openAIParser;
    private readonly int _priority;
    
    public string Name => "OpenAI";
    public int Priority => _priority;
    public bool IsEnabled => true; // OpenAI is always enabled as primary fallback
    public TimeSpan Timeout => TimeSpan.FromSeconds(25);
    
    public OpenAIParserAdapter(
        OpenAIPrescriptionParserAgent openAIParser,
        int priority)
    {
        _openAIParser = openAIParser ?? throw new ArgumentNullException(nameof(openAIParser));
        _priority = priority;
    }
    
    public Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        return _openAIParser.ParsePrescriptionTextAsync(ocrText, cancellationToken);
    }
    
    public Task<bool> HealthCheckAsync()
    {
        return Task.FromResult(_openAIParser != null);
    }
}

/// <summary>
/// Adapter for ClaudePrescriptionParserAgent to implement IPrescriptionParser
/// </summary>
public class ClaudeParserAdapter : IPrescriptionParser
{
    private readonly ClaudePrescriptionParserAgent _claudeParser;
    private readonly bool _isEnabled;
    private readonly int _priority;
    
    public string Name => "Claude";
    public int Priority => _priority;
    public bool IsEnabled => _isEnabled;
    public TimeSpan Timeout => TimeSpan.FromSeconds(20);
    
    public ClaudeParserAdapter(
        ClaudePrescriptionParserAgent claudeParser,
        bool isEnabled,
        int priority)
    {
        _claudeParser = claudeParser ?? throw new ArgumentNullException(nameof(claudeParser));
        _isEnabled = isEnabled;
        _priority = priority;
    }
    
    public Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        return _claudeParser.ParsePrescriptionTextAsync(ocrText, cancellationToken);
    }
    
    public Task<bool> HealthCheckAsync()
    {
        return Task.FromResult(_claudeParser != null);
    }
}
