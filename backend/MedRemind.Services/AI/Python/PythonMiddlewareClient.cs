using MedRemind.Core.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedRemind.Services.AI.Python;

/// <summary>
/// Client for Python Middleware (CrewAI) microservice
/// Calls Python FastAPI service for prescription parsing
/// </summary>
public class PythonMiddlewareClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<PythonMiddlewareClient>? _logger;

    public PythonMiddlewareClient(
        HttpClient httpClient,
        string? baseUrl = null,
        ILogger<PythonMiddlewareClient>? logger = null)
    {
        _httpClient = httpClient;
        // Use baseUrl from HttpClient.BaseAddress if set, otherwise use parameter or default
        _baseUrl = (httpClient.BaseAddress?.ToString() ?? baseUrl ?? "http://localhost:8000").TrimEnd('/');
        _logger = logger;
        
        _logger?.LogInformation($"? Python Middleware Client initialized");
        _logger?.LogInformation($"   Base URL: {_baseUrl}");
    }

    /// <summary>
    /// Parse prescription using Python Middleware service
    /// </summary>
    public async Task<PrescriptionReadResult?> ParsePrescriptionAsync(
        string ocrText,
        string prescriptionId,
        bool saveResult = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation($"?? Calling Python Middleware service...");
            _logger?.LogInformation($"   URL: {_baseUrl}/api/prescription/parse");
            _logger?.LogInformation($"   Prescription ID: {prescriptionId}");

            var request = new
            {
                ocr_text = ocrText,
                prescription_id = prescriptionId,
                save_result = saveResult
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/prescription/parse",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogError($"? Python service error: {response.StatusCode}");
                _logger?.LogError($"   Error: {error}");
                return null;
            }

            var pythonResponse = await response.Content.ReadFromJsonAsync<PythonPrescriptionResponse>(
                cancellationToken: cancellationToken);

            if (pythonResponse == null)
            {
                _logger?.LogError("? Failed to deserialize Python response");
                return null;
            }

            _logger?.LogInformation($"? Python service returned result");
            _logger?.LogInformation($"   Success: {pythonResponse.Success}");
            _logger?.LogInformation($"   Medications: {pythonResponse.Medications?.Count ?? 0}");
            _logger?.LogInformation($"   Processing time: {pythonResponse.ProcessingTime:F2}s");

            // Convert Python response to PrescriptionReadResult
            return ConvertToPrescriptionReadResult(pythonResponse);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to call Python Middleware service");
            return null;
        }
    }

    /// <summary>
    /// Convert Python response to PrescriptionReadResult
    /// </summary>
    private PrescriptionReadResult ConvertToPrescriptionReadResult(PythonPrescriptionResponse pythonResponse)
    {
        var result = new PrescriptionReadResult
        {
            Success = pythonResponse.Success,
            ErrorMessage = pythonResponse.ErrorMessage,
            PrescriptionId = pythonResponse.PrescriptionId,
            PrescriptionDate = !string.IsNullOrEmpty(pythonResponse.PrescriptionDate) 
                ? DateTime.Parse(pythonResponse.PrescriptionDate) 
                : null,
            ProcessingTime = pythonResponse.ProcessingTime,
            CrewSummary = pythonResponse.CrewSummary,
            Warnings = pythonResponse.Warnings ?? new List<string>(),
            Medications = new List<MedicationData>()
        };

        // Convert patient info
        if (pythonResponse.Patient != null)
        {
            result.Patient = new PatientData
            {
                Name = pythonResponse.Patient.Name,
                Age = pythonResponse.Patient.Age,
                Gender = pythonResponse.Patient.Gender
            };
        }

        // Convert doctor info
        if (pythonResponse.Doctor != null)
        {
            result.Doctor = new DoctorData
            {
                Name = pythonResponse.Doctor.Name,
                Specialization = pythonResponse.Doctor.Specialization,
                RegistrationNumber = pythonResponse.Doctor.RegistrationNumber
            };
        }

        // Convert medicine validation
        if (pythonResponse.MedicineValidation != null)
        {
            result.MedicineValidation = new MedicineValidationData
            {
                OverallSafetyScore = pythonResponse.MedicineValidation.OverallSafetyScore,
                RequiresPharmacistReview = pythonResponse.MedicineValidation.RequiresPharmacistReview,
                DuplicateTherapies = pythonResponse.MedicineValidation.DuplicateTherapies ?? new List<string>()
            };

            // Convert drug interactions
            if (pythonResponse.MedicineValidation.DrugInteractions != null)
            {
                result.MedicineValidation.DrugInteractions = pythonResponse.MedicineValidation.DrugInteractions
                    .Select(di => new DrugInteraction
                    {
                        Medicines = di.Medicines ?? new List<string>(),
                        Severity = di.Severity ?? string.Empty,
                        Description = di.Description ?? string.Empty,
                        Recommendation = di.Recommendation
                    })
                    .ToList();
            }

            // Convert safety warnings
            if (pythonResponse.MedicineValidation.SafetyWarnings != null)
            {
                result.MedicineValidation.SafetyWarnings = pythonResponse.MedicineValidation.SafetyWarnings
                    .Select(sw => new SafetyWarning
                    {
                        Medicine = sw.Medicine ?? string.Empty,
                        Type = sw.Type ?? string.Empty,
                        Severity = sw.Severity ?? string.Empty,
                        Message = sw.Message ?? string.Empty,
                        Recommendation = sw.Recommendation
                    })
                    .ToList();
            }
        }

        // Convert medications
        if (pythonResponse.Medications != null)
        {
            foreach (var pythonMed in pythonResponse.Medications)
            {
                var medication = new MedicationData
                {
                    Name = pythonMed.Name,
                    Dosage = pythonMed.Dosage ?? string.Empty,
                    Unit = pythonMed.Unit ?? string.Empty,
                    Frequency = pythonMed.Frequency ?? string.Empty,
                    FrequencyCount = pythonMed.FrequencyCount ?? 0,
                    Duration = pythonMed.Duration,
                    DurationDays = pythonMed.DurationDays ?? 0,
                    Timing = pythonMed.Timing,
                    Instructions = pythonMed.Instructions,
                    ConfidenceScore = pythonMed.ConfidenceScore,
                    
                    // Map Python medicine information to C# properties
                    MedicineDetails = pythonMed.Purpose,  // Python 'purpose' ? C# 'MedicineDetails'
                    SideEffects = pythonMed.SideEffects != null && pythonMed.SideEffects.Any()
                        ? string.Join(", ", pythonMed.SideEffects)  // Convert list to comma-separated string
                        : null,
                    
                    // Map Python age validation to C# properties
                    AgeAppropriate = pythonMed.AgeAppropriate,  // Python 'age_appropriate' ? C# 'AgeAppropriate'
                    AgeSpecificWarning = pythonMed.AgeSpecificWarning  // Python 'age_specific_warning' ? C# 'AgeSpecificWarning'
                };

                result.Medications.Add(medication);
            }
        }

        // Calculate overall confidence score
        if (result.Medications.Any())
        {
            result.ConfidenceScore = result.Medications.Average(m => m.ConfidenceScore ?? 0.0);
        }

        return result;
    }

    /// <summary>
    /// Get saved prescription from Python service
    /// </summary>
    public async Task<PrescriptionReadResult?> GetPrescriptionAsync(
        string prescriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/prescription/{prescriptionId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var pythonResponse = await response.Content.ReadFromJsonAsync<PythonPrescriptionResponse>(
                cancellationToken: cancellationToken);

            return pythonResponse != null ? ConvertToPrescriptionReadResult(pythonResponse) : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to get prescription {prescriptionId}");
            return null;
        }
    }

    /// <summary>
    /// Check Python service health
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/health",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Response from Python CrewAI service
/// </summary>
public class PythonPrescriptionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("prescription_id")]
    public string PrescriptionId { get; set; } = string.Empty;
    
    [JsonPropertyName("patient")]
    public PythonPatient? Patient { get; set; }
    
    [JsonPropertyName("doctor")]
    public PythonDoctor? Doctor { get; set; }
    
    [JsonPropertyName("prescription_date")]
    public string? PrescriptionDate { get; set; }
    
    [JsonPropertyName("medications")]
    public List<PythonMedication> Medications { get; set; } = new();
    
    [JsonPropertyName("medicine_validation")]
    public PythonMedicineValidation? MedicineValidation { get; set; }
    
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
    
    [JsonPropertyName("processing_time")]
    public double ProcessingTime { get; set; }
    
    [JsonPropertyName("crew_summary")]
    public string? CrewSummary { get; set; }
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class PythonPatient
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("age")]
    public int? Age { get; set; }
    
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
}

public class PythonDoctor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }
    
    [JsonPropertyName("registration_number")]
    public string? RegistrationNumber { get; set; }
}

public class PythonMedication
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("dosage")]
    public string? Dosage { get; set; }
    
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
    
    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }
    
    [JsonPropertyName("frequency_count")]
    public int? FrequencyCount { get; set; }
    
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
    
    [JsonPropertyName("duration_days")]
    public int? DurationDays { get; set; }
    
    [JsonPropertyName("timing")]
    public string? Timing { get; set; }
    
    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }
    
    [JsonPropertyName("confidence_score")]
    public double ConfidenceScore { get; set; }
    
    // Medicine information from Python LLM processing
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }
    
    [JsonPropertyName("side_effects")]
    public List<string>? SideEffects { get; set; }
    
    // Age-related safety information from Python LLM validation
    [JsonPropertyName("age_appropriate")]
    public bool? AgeAppropriate { get; set; }
    
    [JsonPropertyName("age_specific_warning")]
    public string? AgeSpecificWarning { get; set; }
}

public class PythonMedicineValidation
{
    [JsonPropertyName("drug_interactions")]
    public List<PythonDrugInteraction> DrugInteractions { get; set; } = new();
    
    [JsonPropertyName("safety_warnings")]
    public List<PythonSafetyWarning> SafetyWarnings { get; set; } = new();
    
    [JsonPropertyName("duplicate_therapies")]
    public List<string> DuplicateTherapies { get; set; } = new();
    
    [JsonPropertyName("overall_safety_score")]
    public double OverallSafetyScore { get; set; }
    
    [JsonPropertyName("requires_pharmacist_review")]
    public bool RequiresPharmacistReview { get; set; }
}

public class PythonDrugInteraction
{
    [JsonPropertyName("medicines")]
    public List<string>? Medicines { get; set; }
    
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }
}

public class PythonSafetyWarning
{
    [JsonPropertyName("medicine")]
    public string? Medicine { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }
}
