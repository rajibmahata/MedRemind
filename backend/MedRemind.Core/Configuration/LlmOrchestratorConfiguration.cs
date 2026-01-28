namespace MedRemind.Core.Configuration;

/// <summary>
/// Configuration for individual LLM provider
/// </summary>
public class LlmProviderConfiguration
{
    public bool Enabled { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Configuration for all LLM providers used in orchestrator
/// </summary>
public class LlmOrchestratorConfiguration
{
    public LlmProviderConfiguration OpenAI { get; set; } = new();
    public LlmProviderConfiguration DeepSeek { get; set; } = new();
    public LlmProviderConfiguration Claude { get; set; } = new();
}
