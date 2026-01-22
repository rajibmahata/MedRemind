using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI;

/// <summary>
/// Placeholder for Claude prescription parser
/// Requires Anthropic.SDK package to be implemented
/// </summary>
public class ClaudePrescriptionParserAgent
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;

    public ClaudePrescriptionParserAgent(
        string apiKey, 
        string model, 
        int maxTokens = 5000) // NEW: Accept maxTokens from config
    {
        _apiKey = apiKey;
        _model = model;
        _maxTokens = maxTokens;
        
        System.Diagnostics.Debug.WriteLine("?? Claude Parser: Not yet implemented");
        System.Diagnostics.Debug.WriteLine("   Requires: Anthropic.SDK NuGet package");
        System.Diagnostics.Debug.WriteLine($"   Model: {_model}");
        System.Diagnostics.Debug.WriteLine($"   Max Tokens: {_maxTokens}");
    }

    public async Task<PrescriptionReadResult> ParsePrescriptionTextAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Placeholder
        
        System.Diagnostics.Debug.WriteLine("? Claude Parser: Not implemented");
        
        // Return empty result
        return new PrescriptionReadResult
        {
            Success = false,
            Medications = new List<MedicationData>()
        };
    }
}
