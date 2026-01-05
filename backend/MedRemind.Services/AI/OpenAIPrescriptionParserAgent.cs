using System.ClientModel;
using System.Text.Json;
using MedRemind.Core.DTOs;
using OpenAI.Chat;

namespace MedRemind.Services.AI;

/// <summary>
/// OpenAI prescription parser agent that processes OCR text
/// Uses OpenAI GPT-4 to extract structured medical data
/// Includes robust error handling and retry logic
/// </summary>
public class OpenAIPrescriptionParserAgent
{
    private readonly ChatClient _chatClient;
    private const int MAX_RETRIES = 3;
    private const int RETRY_DELAY_MS = 1000;

    public OpenAIPrescriptionParserAgent(ChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <summary>
    /// Parse OCR text into structured prescription data with retry logic
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
                System.Diagnostics.Debug.WriteLine($"🔄 OpenAI Parser: Starting prescription parsing... (Attempt {retryCount + 1}/{MAX_RETRIES})");
                System.Diagnostics.Debug.WriteLine($"   OCR text length: {ocrText.Length} characters");

                // Validate input
                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ OpenAI Parser: OCR text is empty");
                    return new PrescriptionParseResult
                    {
                        Success = false,
                        Medications = new List<MedicationData>()
                    };
                }

                var prompt = CreateParserPrompt(ocrText);

                System.Diagnostics.Debug.WriteLine("📤 OpenAI Parser: Sending to OpenAI GPT-4...");

                var chatMessages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a medical prescription parser. Extract structured data accurately and return valid JSON only."),
                    new UserChatMessage(prompt)
                };

                var chatOptions = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 5000,
                    Temperature = 0.1f, // Lower temperature for more consistent parsing
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                };

                // Add timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

                ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(
                    chatMessages, 
                    chatOptions, 
                    cts.Token);

                var completion = response.Value;
                
                System.Diagnostics.Debug.WriteLine($"✅ OpenAI Parser: Response received");
                System.Diagnostics.Debug.WriteLine($"   Finish reason: {completion.FinishReason}");
                System.Diagnostics.Debug.WriteLine($"   Tokens used: {completion.Usage.TotalTokenCount}");

                if (completion.Content == null || completion.Content.Count == 0)
                {
                    throw new InvalidOperationException("No response content from OpenAI");
                }

                var content = completion.Content[0].Text;
                System.Diagnostics.Debug.WriteLine($"📥 OpenAI Parser: Got response");
                System.Diagnostics.Debug.WriteLine($"   Content preview: {content.Substring(0, Math.Min(300, content.Length))}...");

                // Parse the structured JSON response
                var result = ParseStructuredData(content);

                System.Diagnostics.Debug.WriteLine($"✅ OpenAI Parser: Parsing complete");
                System.Diagnostics.Debug.WriteLine($"   Patient: {result.Patient?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Doctor: {result.Doctor?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Medications: {result.Medications?.Count ?? 0}");

                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                System.Diagnostics.Debug.WriteLine($"⏱️ OpenAI Parser: Timeout on attempt {retryCount + 1}");
                lastException = new TimeoutException("OpenAI request timed out after 30 seconds");
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    System.Diagnostics.Debug.WriteLine($"⏳ Waiting {RETRY_DELAY_MS}ms before retry...");
                    await Task.Delay(RETRY_DELAY_MS * retryCount, cancellationToken); // Exponential backoff
                }
            }
            catch (ClientResultException ex) when (ex.Status >= 500 || ex.Status == 429)
            {
                // Server error or rate limit - retry
                System.Diagnostics.Debug.WriteLine($"⚠️ OpenAI Parser: Retryable error on attempt {retryCount + 1}");
                System.Diagnostics.Debug.WriteLine($"   Status: {ex.Status} - {ex.Message}");
                lastException = ex;
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    int delayMs = RETRY_DELAY_MS * retryCount;
                    System.Diagnostics.Debug.WriteLine($"⏳ Waiting {delayMs}ms before retry...");
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
            catch (ClientResultException ex)
            {
                // Client error (4xx) - don't retry
                System.Diagnostics.Debug.WriteLine($"❌ OpenAI Parser: Client error (no retry)");
                System.Diagnostics.Debug.WriteLine($"   Status: {ex.Status}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                
                return new PrescriptionParseResult
                {
                    Success = false,
                    Medications = new List<MedicationData>()
                };
            }
            catch (HttpRequestException ex)
            {
                // Network error - retry
                System.Diagnostics.Debug.WriteLine($"⚠️ OpenAI Parser: Network error on attempt {retryCount + 1}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                lastException = ex;
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    int delayMs = RETRY_DELAY_MS * retryCount;
                    System.Diagnostics.Debug.WriteLine($"⏳ Waiting {delayMs}ms before retry...");
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OpenAI Parser: Unexpected error: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                lastException = ex;
                break; // Don't retry on unexpected errors
            }
        }

        // All retries failed
        System.Diagnostics.Debug.WriteLine($"❌ OpenAI Parser: All {MAX_RETRIES} attempts failed");
        System.Diagnostics.Debug.WriteLine($"   Last error: {lastException?.Message}");
        
        return new PrescriptionParseResult
        {
            Success = false,
            Medications = new List<MedicationData>()
        };
    }

    private string CreateParserPrompt(string ocrText)
    {
        return $@"You are a medical prescription parser.

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
            System.Diagnostics.Debug.WriteLine("🔍 OpenAI Parser: Parsing structured JSON...");

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
                // PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Keep commented out
            };

            var data = JsonSerializer.Deserialize<PrescriptionStructuredData>(jsonContent, options);

            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ OpenAI Parser: Deserialization returned null");
                System.Diagnostics.Debug.WriteLine($"   JSON content: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
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
            System.Diagnostics.Debug.WriteLine($"❌ OpenAI Parser: JSON parsing error");
            System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Path: {ex.Path}");
            System.Diagnostics.Debug.WriteLine($"   JSON snippet: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
            
            return new PrescriptionParseResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ OpenAI Parser: Unexpected parsing error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            
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
            System.Diagnostics.Debug.WriteLine("⚠️ OpenAI Parser: No medications found");
            return new List<MedicationData>();
        }

        System.Diagnostics.Debug.WriteLine($"✅ OpenAI Parser: Converting {medications.Count} medication(s)");

        return medications.Select(m => new MedicationData
        {
            Name = m.Name ?? "Unknown",
            Dosage = m.Dosage ?? "0",
            Unit = m.Unit ?? "tablet",
            Frequency = m.Frequency ?? "Once daily",
            FrequencyCount = m.FrequencyCount ?? 1,  // Use ?? operator for nullable int
            DurationDays = m.DurationDays ?? 7,      // Use ?? operator for nullable int (default 7 days)
            Instructions = BuildInstructions(m),
            ConfidenceScore = m.ConfidenceScore ?? 0.5  // Use ?? operator for nullable double
        }).ToList();
    }

    private string BuildInstructions(MedicationStructuredData med)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(med.Timing))
        {
            parts.Add(med.Timing);
        }

        if (!string.IsNullOrEmpty(med.Instructions))
        {
            parts.Add(med.Instructions);
        }

        if (!string.IsNullOrEmpty(med.Duration))
        {
            parts.Add($"Duration: {med.Duration}");
        }

        return parts.Any() ? string.Join(". ", parts) : "No special instructions";
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
        public int? FrequencyCount { get; set; }  // ← Made nullable
        public string? Duration { get; set; }
        public int? DurationDays { get; set; }    // ← Made nullable (FIX!)
        public string? Timing { get; set; }
        public string? Instructions { get; set; }
        public double? ConfidenceScore { get; set; }  // ← Made nullable
    }
}

/// <summary>
/// Result from prescription parsing
/// </summary>
public class PrescriptionParseResult
{
    public bool Success { get; set; }
    public PatientData? Patient { get; set; }
    public DoctorData? Doctor { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public List<MedicationData> Medications { get; set; } = new();
}

/// <summary>
/// Patient information
/// </summary>
public class PatientData
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
}

/// <summary>
/// Doctor information
/// </summary>
public class DoctorData
{
    public string? Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Specialization { get; set; }
}
