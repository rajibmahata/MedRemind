using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedRemind.Core.DTOs;

namespace MedRemind.Services.AI;

/// <summary>
/// DeepSeek AI-based medical prescription parser
/// DeepSeek is cost-effective and provides high-quality parsing for medical text
/// API: https://api.deepseek.com/v1/chat/completions
/// </summary>
public class DeepSeekPrescriptionParserAgent
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _model = "deepseek-chat"; // DeepSeek Chat model
    private const int MAX_RETRIES = 2; // Reduced from 3 to 2 for faster processing
    private const int RETRY_DELAY_MS = 500; // Reduced from 1000ms to 500ms

    public DeepSeekPrescriptionParserAgent(HttpClient httpClient, string apiKey, string apiUrl = "https://api.deepseek.com/chat/completions")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _apiUrl = apiUrl;
        
        System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: Initialized");
        System.Diagnostics.Debug.WriteLine($"   Model: {_model}");
        System.Diagnostics.Debug.WriteLine($"   API URL: {_apiUrl}");
    }

    /// <summary>
    /// Parse OCR text into structured prescription data using DeepSeek
    /// </summary>
    public async Task<PrescriptionParseResult> ParsePrescriptionTextAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        Exception? lastException = null;

        while (retryCount < MAX_RETRIES)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? DeepSeek Parser: Starting (Attempt {retryCount + 1}/{MAX_RETRIES})");
                System.Diagnostics.Debug.WriteLine($"   OCR text: {ocrText.Length} characters");

                // Validate input
                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    System.Diagnostics.Debug.WriteLine("?? DeepSeek Parser: OCR text is empty");
                    return new PrescriptionParseResult
                    {
                        Success = false,
                        Medications = new List<MedicationData>()
                    };
                }

                var prompt = CreateParserPrompt(ocrText);

                System.Diagnostics.Debug.WriteLine("?? DeepSeek Parser: Sending to DeepSeek API...");

                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a medical prescription parser expert. Extract structured data accurately and return valid JSON only."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    max_tokens = 5000,
                    temperature = 0.1, // Low temperature for consistent parsing
                    response_format = new { type = "json_object" }
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsJsonAsync(
                    _apiUrl,
                    requestBody,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    System.Diagnostics.Debug.WriteLine($"? DeepSeek API error: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"   Error: {errorContent}");
                    throw new HttpRequestException($"DeepSeek API error: {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<DeepSeekApiResponse>(jsonResponse);

                if (result?.Choices == null || result.Choices.Count == 0)
                {
                    throw new InvalidOperationException("No response content from DeepSeek");
                }

                var content = result.Choices[0].Message.Content;
                
                System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: Response received");
                System.Diagnostics.Debug.WriteLine($"   Content length: {content.Length}");

                // Parse the structured JSON response
                var parseResult = ParseStructuredData(content);

                System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: Parsing complete");
                System.Diagnostics.Debug.WriteLine($"   Patient: {parseResult.Patient?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Doctor: {parseResult.Doctor?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Medications: {parseResult.Medications?.Count ?? 0}");

                return parseResult;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine($"?? DeepSeek Parser: Timeout on attempt {retryCount + 1}");
                lastException = new TimeoutException("DeepSeek request timed out after 30 seconds");
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    System.Diagnostics.Debug.WriteLine($"? Waiting {RETRY_DELAY_MS}ms before retry...");
                    await Task.Delay(RETRY_DELAY_MS * retryCount, cancellationToken);
                }
            }
            catch (Exception ex) when (IsRetryableError(ex))
            {
                System.Diagnostics.Debug.WriteLine($"?? DeepSeek Parser: Retryable error on attempt {retryCount + 1}");
                System.Diagnostics.Debug.WriteLine($"   Error: {ex.Message}");
                lastException = ex;
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    int delayMs = RETRY_DELAY_MS * retryCount;
                    System.Diagnostics.Debug.WriteLine($"? Waiting {delayMs}ms before retry...");
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: Non-retryable error: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                lastException = ex;
                break;
            }
        }

        // All retries failed
        System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: All {MAX_RETRIES} attempts failed");
        System.Diagnostics.Debug.WriteLine($"   Last error: {lastException?.Message}");
        
        return new PrescriptionParseResult
        {
            Success = false,
            Medications = new List<MedicationData>()
        };
    }

    private string CreateParserPrompt(string ocrText)
    {
        return $@"You are a medical prescription parser. Extract structured data from the prescription text below.

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
""""""

Rules:
1. Extract ALL medications found
2. Use standard medical terminology
3. Set confidenceScore based on text clarity and completeness
4. If a field is unclear, use null and lower confidence
5. Convert duration to days (7 days, 14 days, etc.)
6. Standardize frequency (Once daily, Twice daily, Three times daily, Four times daily)
7. Return empty medications array if none found";
    }

    private PrescriptionParseResult ParseStructuredData(string jsonContent)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? DeepSeek Parser: Parsing structured JSON...");

            // Remove markdown code blocks if present
            jsonContent = jsonContent.Trim();
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Substring(7);
            }
            if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Substring(3);
            }
            if (jsonContent.EndsWith("```"))
            {
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
            }
            jsonContent = jsonContent.Trim();

            System.Diagnostics.Debug.WriteLine($"   JSON length: {jsonContent.Length}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var data = JsonSerializer.Deserialize<PrescriptionStructuredData>(jsonContent, options);

            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("?? DeepSeek Parser: Deserialization returned null");
                return new PrescriptionParseResult
                {
                    Success = false,
                    Medications = new List<MedicationData>()
                };
            }

            // Convert to PrescriptionParseResult
            var result = new PrescriptionParseResult
            {
                Success = true,
                Patient = data.Patient,
                Doctor = data.Doctor,
                PrescriptionDate = ParseDate(data.PrescriptionDate),
                Medications = ConvertMedications(data.Medications)
            };

            return result;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: JSON parsing error");
            System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Path: {ex.Path}");
            
            return new PrescriptionParseResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: Unexpected parsing error: {ex.Message}");
            
            return new PrescriptionParseResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
    }

    private List<MedicationData> ConvertMedications(List<MedicationStructuredData>? medications)
    {
        if (medications == null || !medications.Any())
        {
            System.Diagnostics.Debug.WriteLine("?? DeepSeek Parser: No medications found");
            return new List<MedicationData>();
        }

        System.Diagnostics.Debug.WriteLine($"?? DeepSeek Parser: Converting {medications.Count} medication(s)");

        return medications.Select(m => new MedicationData
        {
            Name = m.Name ?? "Unknown",
            Dosage = m.Dosage ?? "0",
            Unit = m.Unit ?? "tablet",
            Frequency = m.Frequency ?? "Once daily",
            FrequencyCount = m.FrequencyCount > 0 ? m.FrequencyCount : 1,
            DurationDays = m.DurationDays > 0 ? m.DurationDays : 7,
            Instructions = BuildInstructions(m),
            ConfidenceScore = m.ConfidenceScore > 0 ? m.ConfidenceScore : 0.5
        }).ToList();
    }

    private string BuildInstructions(MedicationStructuredData med)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(med.Timing))
            parts.Add(med.Timing);

        if (!string.IsNullOrEmpty(med.Instructions))
            parts.Add(med.Instructions);

        if (!string.IsNullOrEmpty(med.Duration))
            parts.Add($"Duration: {med.Duration}");

        return parts.Any() ? string.Join(". ", parts) : "No special instructions";
    }

    private bool IsRetryableError(Exception ex)
    {
        // Retry on network errors, timeouts, and rate limits
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex.Message.Contains("429") ||
               ex.Message.Contains("500") ||
               ex.Message.Contains("503");
    }

    // Response models
    private class DeepSeekApiResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public List<DeepSeekChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public DeepSeekUsage? Usage { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }
    }

    private class DeepSeekChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public DeepSeekMessage Message { get; set; } = new();

        [JsonPropertyName("logprobs")]
        public object? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;
    }

    private class DeepSeekMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class DeepSeekUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_tokens_details")]
        public DeepSeekPromptTokensDetails? PromptTokensDetails { get; set; }

        [JsonPropertyName("prompt_cache_hit_tokens")]
        public int? PromptCacheHitTokens { get; set; }

        [JsonPropertyName("prompt_cache_miss_tokens")]
        public int? PromptCacheMissTokens { get; set; }
    }

    private class DeepSeekPromptTokensDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }

    // Structured data models
    private class PrescriptionStructuredData
    {
        public PatientData? Patient { get; set; }
        public DoctorData? Doctor { get; set; }
        public string? PrescriptionDate { get; set; }
        public List<MedicationStructuredData>? Medications { get; set; }
    }

    private class MedicationStructuredData
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

    
}
