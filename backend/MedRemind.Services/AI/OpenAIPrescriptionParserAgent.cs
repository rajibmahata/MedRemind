using System.ClientModel;
using System.Text.Json;
using MedRemind.Core.DTOs;
using MedRemind.Services.AI.Agents;
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
    public async Task<PrescriptionReadResult> ParsePrescriptionTextAsync(
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
                    return new PrescriptionReadResult
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
                
                return new PrescriptionReadResult
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
        
        return new PrescriptionReadResult
        {
            Success = false,
            Medications = new List<MedicationData>()
        };
    }

    private string CreateParserPrompt(string ocrText)
    {
        return $@"You are a medical prescription parser specialized in interpreting handwritten prescriptions.

Extract structured data from the prescription text below.

Return ONLY valid JSON (no markdown, no code blocks).

IMPORTANT: Recognize common handwritten prescription formats:

1. DOSE COUNT NOTATION (numbers indicate frequency):
   CRITICAL: When you see ONLY zeros, it means ""take X times daily"" NOT ""skip""!
   - '0 0 0' = Three times daily (Morning, Afternoon, Evening) - dose to be specified
   - '0 0' = Twice daily (Morning, Evening) - dose to be specified  
   - '0' = Once daily (Evening) - dose to be specified
   - '1 1 1' = 1 unit three times daily (morning, afternoon, evening)
   - '2 1 1' = Variable dosing: 2 units morning, 1 unit afternoon, 1 unit evening
   - '1 0 0' = 1 unit once daily (morning only)

2. BINARY NOTATION (1 = take, 0 = skip) - when mixed with 1s:
   - '1-0-0' = Take once daily in morning (skip afternoon/evening)
   - '1-1-0' = Twice daily (morning and afternoon)
   - '1-0-1' = Twice daily (morning and evening)
   - '1-1-1' = Three times daily

   HOW TO DISTINGUISH:
   - ONLY zeros (0 0 0, 0 0, 0) → DOSE COUNT = frequency indicator
   - Mix of 1s and 0s (1-0-1) → BINARY = take/skip pattern
   - Numbers > 1 (2-1-1) → DOSE COUNT = variable dosing

3. MEDICAL ABBREVIATIONS:
   - 'OD' = Once Daily
   - 'BD' = Twice Daily  
   - 'TDS' or 'TID' = Three times daily
   - 'QDS' or 'QID' = Four times daily
   - 'AC' = Before meals
   - 'PC' = After meals
   - 'HS' = At bedtime
   - 'PRN' or 'SOS' = As needed (set frequencyCount to 0)
   - 'STAT' = Immediately

4. CONDITIONAL INSTRUCTIONS (SOS/PRN):
   - 'SOS if fever' = As needed if fever
   - 'SOS if fever > 100°F' = As needed if fever exceeds 100°F
   - 'PRN for pain' = As needed for pain
   - Set frequencyCount: 0 for SOS/PRN medications

JSON Structure:
{{
  ""patient"": {{
    ""name"": ""string or null"",
    ""age"": ""number or null"",
    ""gender"": ""string or null""
  }},
  ""doctor"": {{
    ""name"": ""string or null"",
    ""registration_number"": ""string or null"",
    ""specialization"": ""string or null""
  }},
  ""prescription_date"": ""YYYY-MM-DD or null"",
  ""medications"": [
    {{
      ""name"": ""string (required)"",
      ""dosage"": ""string (e.g., '500', '10')"",
      ""unit"": ""string (e.g., 'mg', 'ml', 'tablet')"",
      ""frequency"": ""string (e.g., 'Three times daily', 'Twice daily after meals')"",
      ""frequencyCount"": ""number (1-4 for regular, 0 for SOS/PRN)"",
      ""duration"": ""string (e.g., '7 days', '2 weeks')"",
      ""durationDays"": ""number (convert to days, null if indefinite)"",
      ""timing"": ""string (e.g., 'Morning, afternoon, evening', 'After meals')"",
      ""instructions"": ""string (complete instructions)"",
      ""confidenceScore"": ""number 0.0-1.0 (based on clarity)""
    }}
  ]
}}

Prescription Text:
""""""
{ocrText}
""""""

PARSING RULES:

1. DOSE COUNT NOTATION (CRITICAL):
   - '0 0 0' → frequency: ""Three times daily"", frequencyCount: 3, timing: ""Morning, afternoon, and evening""
   - '0 0' → frequency: ""Twice daily"", frequencyCount: 2, timing: ""Morning and evening""
   - '0' → frequency: ""Once daily"", frequencyCount: 1, timing: ""Evening""
   - '1 1 1' → frequency: ""Three times daily (1 unit per dose)"", frequencyCount: 3
   - '2 1 1' → frequency: ""Three times daily (variable)"", frequencyCount: 3, instructions: ""2 units morning, 1 unit afternoon, 1 unit evening""

2. BINARY NOTATION:
   - '1-0-0' → frequency: ""Once daily (morning)"", frequencyCount: 1, timing: ""Morning""
   - '1-1-0' → frequency: ""Twice daily"", frequencyCount: 2, timing: ""Morning and afternoon""
   - '1-1-1' → frequency: ""Three times daily"", frequencyCount: 3, timing: ""Morning, afternoon, and evening""

3. ABBREVIATIONS:
   - 'OD' → frequency: ""Once daily"", frequencyCount: 1
   - 'BD' → frequency: ""Twice daily"", frequencyCount: 2
   - 'TDS/TID' → frequency: ""Three times daily"", frequencyCount: 3
   - 'QDS/QID' → frequency: ""Four times daily"", frequencyCount: 4
   - 'AC' → timing: ""Before meals""
   - 'PC' → timing: ""After meals""
   - 'HS' → timing: ""At bedtime""

4. SOS/PRN:
   - Set frequencyCount: 0
   - Include condition in frequency and instructions
   - Example: 'SOS if fever > 100°F' → frequency: ""As needed if fever exceeds 100°F"", frequencyCount: 0

5. GENERAL:
   - Extract ALL medications
   - Convert duration to days (1 week = 7, 1 month = 30)
   - If SOS/PRN, set durationDays to null
   - Combine timing and frequency naturally
   - Return empty array if no medications found

EXAMPLES:

Example 1 - Dose Count: ""Tab Paracetamol 500mg 0 0 0 x 3 days""
→ {{
  ""name"": ""Paracetamol"",
  ""dosage"": ""500"",
  ""unit"": ""mg"",
  ""frequency"": ""Three times daily"",
  ""frequencyCount"": 3,
  ""timing"": ""Morning, afternoon, and evening"",
  ""durationDays"": 3,
  ""confidenceScore"": 0.95
}}

Example 2 - Binary: ""Tab Aspirin 75mg 1-0-0 AC x 30 days""
→ {{
  ""name"": ""Aspirin"",
  ""dosage"": ""75"",
  ""unit"": ""mg"",
  ""frequency"": ""Once daily (morning before meals)"",
  ""frequencyCount"": 1,
  ""timing"": ""Morning before meals"",
  ""durationDays"": 30,
  ""confidenceScore"": 0.95
}}

Example 3 - SOS: ""Tab Crocin 650mg SOS if fever > 100°F""
→ {{
  ""name"": ""Crocin"",
  ""dosage"": ""650"",
  ""unit"": ""mg"",
  ""frequency"": ""As needed if fever exceeds 100°F"",
  ""frequencyCount"": 0,
  ""timing"": ""If needed"",
  ""instructions"": ""Take only if fever exceeds 100 degrees Fahrenheit"",
  ""durationDays"": null,
  ""confidenceScore"": 0.9
}}";
    }

    private PrescriptionReadResult ParseStructuredData(string jsonContent)
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

            var data = JsonSerializer.Deserialize<PrescriptionReadResult>(jsonContent, options);

            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ OpenAI Parser: Deserialization returned null");
                System.Diagnostics.Debug.WriteLine($"   JSON content: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
                return new PrescriptionReadResult
                {
                    Success = false,
                    Medications = new List<MedicationData>()
                };
            }

            // Convert to PrescriptionReadResult
            var result = new PrescriptionReadResult
            {
                Success = true,
                Patient = data.Patient,
                Doctor = data.Doctor,
                PrescriptionDate = data.PrescriptionDate,
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
            
            return new PrescriptionReadResult
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
            
            return new PrescriptionReadResult
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

    private List<MedicationData> ConvertMedications(List<MedicationData>? medications)
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

    private string BuildInstructions(MedicationData med)
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
   

   
}




