using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI;

/// <summary>
/// Placeholder for Claude prescription parser
/// Requires Anthropic.SDK package to be implemented
/// </summary>
public class ClaudePrescriptionParserAgent
{
    public ClaudePrescriptionParserAgent(string apiKey, string model)
    {
        System.Diagnostics.Debug.WriteLine("?? Claude Parser: Not yet implemented");
        System.Diagnostics.Debug.WriteLine("   Requires: Anthropic.SDK NuGet package");
    }

    public async Task<PrescriptionParseResult> ParsePrescriptionTextAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Placeholder
        
        System.Diagnostics.Debug.WriteLine("? Claude Parser: Not implemented");
        
        // Return empty result
        return new PrescriptionParseResult
        {
            Success = false,
            Medications = new List<MedicationData>()
        };
    }
}
