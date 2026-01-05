using MedRemind.Services.AI.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.AI;

/// <summary>
/// Registry for managing available prescription parsers dynamically
/// Enables runtime parser registration and discovery
/// </summary>
public class ParserRegistry
{
    private readonly List<IPrescriptionParser> _parsers = new();
    private readonly ILogger<ParserRegistry> _logger;
    
    public ParserRegistry(ILogger<ParserRegistry> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Register a new parser
    /// </summary>
    public void Register(IPrescriptionParser parser)
    {
        if (parser == null)
        {
            _logger.LogWarning("Attempted to register null parser. Skipping.");
            return;
        }
        
        if (_parsers.Any(p => p.Name == parser.Name))
        {
            _logger.LogWarning($"Parser {parser.Name} already registered. Skipping.");
            return;
        }
        
        _parsers.Add(parser);
        _logger.LogInformation($"? Registered parser: {parser.Name} (Priority: {parser.Priority}, Timeout: {parser.Timeout.TotalSeconds}s)");
    }
    
    /// <summary>
    /// Get all enabled parsers ordered by priority
    /// </summary>
    public IEnumerable<IPrescriptionParser> GetEnabledParsers()
    {
        return _parsers
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority);
    }
    
    /// <summary>
    /// Get a specific parser by name
    /// </summary>
    public IPrescriptionParser? GetParser(string name)
    {
        return _parsers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Total number of registered parsers
    /// </summary>
    public int Count => _parsers.Count;
    
    /// <summary>
    /// Number of enabled parsers
    /// </summary>
    public int EnabledCount => _parsers.Count(p => p.IsEnabled);
    
    /// <summary>
    /// Get all parser names
    /// </summary>
    public IEnumerable<string> GetParserNames()
    {
        return _parsers.Select(p => p.Name);
    }
}
