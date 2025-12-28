using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json;
using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI.Agents;

/// <summary>
/// Agent 2: Extracts structured prescription data using OpenAI
/// </summary>
public class PrescriptionDataExtractionAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;

    public PrescriptionDataExtractionAgent(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <summary>
    /// Extract structured prescription data from OCR text using OpenAI
    /// </summary>
    [KernelFunction("extract_prescription_data")]
    [Description("Extracts structured patient, prescription, and medication data from OCR text using AI")]
    public async Task<ExtractionResult> ExtractPrescriptionDataAsync(
        [Description("OCR extracted text from prescription")] string ocrText,
        [Description("Specificity level: normal, detailed, or strict")] string specificityLevel = "normal",
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Extraction Agent: Starting...");
            System.Diagnostics.Debug.WriteLine($"   OCR text length: {ocrText?.Length ?? 0} characters");
            System.Diagnostics.Debug.WriteLine($"   Specificity: {specificityLevel}");

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                return new ExtractionResult
                {
                    Success = false,
                    ErrorMessage = "OCR text is empty"
                };
            }

            var prompt = CreateExtractionPrompt(ocrText, specificityLevel);
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a medical prescription data extraction expert. Extract structured data accurately and return valid JSON only.");
            chatHistory.AddUserMessage(prompt);

            var executionSettings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
            {
                MaxTokens = 5000,
                Temperature = 0.1,
                ResponseFormat = "json_object" // Force JSON response
            };

            System.Diagnostics.Debug.WriteLine("?? Extraction Agent: Calling OpenAI...");

            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            var jsonContent = response.Content ?? string.Empty;

            System.Diagnostics.Debug.WriteLine($"?? Extraction Agent: Response received");
            System.Diagnostics.Debug.WriteLine($"   Content length: {jsonContent.Length} characters");

            // Parse JSON response
            var structuredData = ParseStructuredData(jsonContent);

            if (structuredData == null)
            {
                return new ExtractionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse structured data from OpenAI response"
                };
            }

            System.Diagnostics.Debug.WriteLine($"? Extraction Agent: Success");
            System.Diagnostics.Debug.WriteLine($"   Patient: {structuredData.Patient?.Name ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Doctor: {structuredData.Doctor?.Name ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Medications: {structuredData.Medications?.Count ?? 0}");

            return new ExtractionResult
            {
                Success = true,
                StructuredData = structuredData,
                RawJsonResponse = jsonContent,
                SpecificityLevel = specificityLevel
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Extraction Agent: Error - {ex.Message}");
            return new ExtractionResult
            {
                Success = false,
                ErrorMessage = $"Extraction failed: {ex.Message}"
            };
        }
    }

    private string CreateExtractionPrompt(string ocrText, string specificityLevel)
    {
        var basePrompt = $@"You are a medical prescription parser.

Extract structured data from the prescription text below.

Return ONLY valid JSON (no markdown, no code blocks).

Fields:
- patient:
  - name (string)
  - age (number or null)
  - gender (string or null)
- doctor:
  - name (string)
  - registration_number (string or null)
  - specialization (string or null)
- prescription_date (YYYY-MM-DD format or null)
- medications: [
    {{
      name (string, required),
      dosage (string, e.g., ""500""),
      unit (string, e.g., ""mg"", ""tablet"", ""ml""),
      frequency (string, e.g., ""Twice daily"", ""Three times daily""),
      frequencyCount (number, 1-4),
      duration (string, e.g., ""7 days"", ""2 weeks""),
      durationDays (number),
      timing (string, e.g., ""after meals"", ""before breakfast""),
      instructions (string, additional notes),
      confidenceScore (number 0.0-1.0, based on clarity)
    }}
  ]

Prescription Text:
""""""
{ocrText}
""""""";

        // Add specificity instructions based on level
        var specificityInstructions = specificityLevel.ToLowerInvariant() switch
        {
            "detailed" => @"

DETAILED EXTRACTION MODE:
- Extract EVERY field mentioned, even partial information
- Include all medications, even if dosage/frequency unclear
- Set lower confidenceScore for unclear fields
- Extract all doctor details (name, registration, specialization)
- Include any additional notes or instructions",

            "strict" => @"

STRICT EXTRACTION MODE:
- ONLY extract fields that are CLEARLY visible and readable
- Skip medications with unclear dosage or frequency
- Use null for any ambiguous fields
- Set high confidenceScore (>0.8) only for crystal-clear data
- Double-check all numeric values (age, dosage, duration)
- Verify medication names against common drug lists",

            _ => @"

NORMAL EXTRACTION MODE:
- Extract clearly visible information
- Use best judgment for slightly unclear fields
- Balance completeness with accuracy
- Set appropriate confidenceScore (0.0-1.0)"
        };

        return basePrompt + specificityInstructions + @"

Rules:
1. Extract ALL medications found
2. Use standard medical terminology
3. Set confidenceScore based on text clarity and completeness
4. If a field is unclear, use null and lower confidence
5. Convert duration to days (7 days, 14 days, etc.)
6. Standardize frequency (Once daily, Twice daily, Three times daily, Four times daily)
7. Return empty medications array if no medications clearly visible";
    }

    private PrescriptionStructuredData? ParseStructuredData(string jsonContent)
    {
        try
        {
            // Remove markdown code blocks if present
            jsonContent = jsonContent.Trim();
            if (jsonContent.StartsWith("```json"))
                jsonContent = jsonContent.Substring(7);
            if (jsonContent.StartsWith("```"))
                jsonContent = jsonContent.Substring(3);
            if (jsonContent.EndsWith("```"))
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
            jsonContent = jsonContent.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<PrescriptionStructuredData>(jsonContent, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? JSON parsing error: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Result from prescription data extraction
/// </summary>
public class ExtractionResult
{
    public bool Success { get; set; }
    public PrescriptionStructuredData? StructuredData { get; set; }
    public string? RawJsonResponse { get; set; }
    public string? SpecificityLevel { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Structured prescription data model
/// </summary>
public class PrescriptionStructuredData
{
    public PatientData? Patient { get; set; }
    public DoctorData? Doctor { get; set; }
    public string? PrescriptionDate { get; set; }
    public List<MedicationStructuredData>? Medications { get; set; }
}

/// <summary>
/// Medication structured data model
/// </summary>
public class MedicationStructuredData
{
    public string? Name { get; set; }
    public string? Dosage { get; set; }
    public string? Unit { get; set; }
    public string? Frequency { get; set; }
    public int FrequencyCount { get; set; }
    public string? Duration { get; set; }
    public int DurationDays { get; set; }
    public string? Timing { get; set; }
    public string? Instructions { get; set; }
    public double ConfidenceScore { get; set; }
}
