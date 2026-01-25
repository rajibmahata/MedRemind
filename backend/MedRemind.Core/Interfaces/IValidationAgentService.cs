using MedRemind.Core.DTOs;

namespace MedRemind.Core.Interfaces;

/// <summary>
/// Service for validating medications
/// </summary>
public interface IValidationAgentService
{
    /// <summary>
    /// Validate list of medications
    /// </summary>
    Task<List<ValidationWarning>> ValidateMedicationsAsync(List<MedicationData> medications, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate confidence score for a medication
    /// </summary>
    double CalculateConfidenceScore(MedicationData medication);
}
