using MedRemind.Core.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

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
            PrescriptionDate = !string.IsNullOrEmpty(pythonResponse.PrescriptionDate) 
                ? DateTime.Parse(pythonResponse.PrescriptionDate) 
                : null,
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
                    ConfidenceScore = pythonMed.ConfidenceScore
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
