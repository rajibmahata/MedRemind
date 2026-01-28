using MedRemind.Core.DTOs;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedRemind.Services.AI;

/// <summary>
/// Claude AI-based medical prescription parser (Anthropic)
/// Claude excels at understanding context and handwritten text
/// API: https://api.anthropic.com/v1/messages
/// </summary>
public class ClaudePrescriptionParserAgent
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private const string API_URL = "https://api.anthropic.com/v1/messages";
    private const int MAX_RETRIES = 2;
    private const int RETRY_DELAY_MS = 500;

    public ClaudePrescriptionParserAgent(
        HttpClient httpClient,
        string apiKey, 
        string model, 
        int maxTokens = 1500)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey;
        _model = model;
        _maxTokens = maxTokens;
        
        System.Diagnostics.Debug.WriteLine("? Claude Parser: Initialized");
        System.Diagnostics.Debug.WriteLine($"   Model: {_model}");
        System.Diagnostics.Debug.WriteLine($"   Max Tokens: {_maxTokens}");
    }

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
                System.Diagnostics.Debug.WriteLine($"?? Claude Parser: Starting (Attempt {retryCount + 1}/{MAX_RETRIES})");
                
                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    System.Diagnostics.Debug.WriteLine("?? Claude Parser: OCR text is empty");
                    return new PrescriptionReadResult
                    {
                        Success = false,
                        Medications = new List<MedicationData>()
                    };
                }

                var prompt = CreateParserPrompt(ocrText);
                
                System.Diagnostics.Debug.WriteLine("?? Claude Parser: Sending to Claude API...");

                var requestBody = new
                {
                    model = _model,
                    max_tokens = _maxTokens,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    system = "You are a medical prescription parser expert. Extract structured data accurately and return valid JSON only.",
                    temperature = 0.0
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    System.Diagnostics.Debug.WriteLine($"? Claude API error: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"   Error: {errorContent}");
                    throw new HttpRequestException($"Claude API error: {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<ClaudeApiResponse>(jsonResponse);

                if (result?.Content == null || result.Content.Count == 0)
                {
                    throw new InvalidOperationException("No response content from Claude");
                }

                var content = result.Content[0].Text;
                
                System.Diagnostics.Debug.WriteLine($"? Claude Parser: Response received");
                System.Diagnostics.Debug.WriteLine($"   Content length: {content.Length}");

                var parseResult = ParseStructuredData(content);

                System.Diagnostics.Debug.WriteLine($"? Claude Parser: Parsing complete");
                System.Diagnostics.Debug.WriteLine($"   Medications: {parseResult.Medications?.Count ?? 0}");

                return parseResult;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine($"?? Claude Parser: Timeout on attempt {retryCount + 1}");
                lastException = new TimeoutException("Claude request timed out");
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    await Task.Delay(RETRY_DELAY_MS * retryCount, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (IsRetryableError(ex))
            {
                System.Diagnostics.Debug.WriteLine($"?? Claude Parser: Retryable error on attempt {retryCount + 1}");
                lastException = ex;
                retryCount++;
                
                if (retryCount < MAX_RETRIES)
                {
                    await Task.Delay(RETRY_DELAY_MS * retryCount, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Claude Parser: Non-retryable error: {ex.Message}");
                lastException = ex;
                break;
            }
        }

        System.Diagnostics.Debug.WriteLine($"? Claude Parser: All {MAX_RETRIES} attempts failed");
        
        return new PrescriptionReadResult
        {
            Success = false,
            Medications = new List<MedicationData>()
        };
    }

    private string CreateParserPrompt(string ocrText)
    {
        return $@"You are a medical prescription parser specialized in handwritten prescriptions.

Extract structured data and return ONLY valid JSON.

IMPORTANT: Recognize handwritten prescription formats:

1. DOSE COUNT NOTATION (frequency indicator):
   CRITICAL: '0 0 0' means THREE TIMES DAILY, NOT skip!
   - '0 0 0' = Three times daily (Morning, Afternoon, Evening)
   - '0 0' = Twice daily (Morning, Evening)
   - '0' = Once daily (Evening)
   - '1 1 1' = 1 unit three times daily
   - '2 1 1' = 2 units morning, 1 afternoon, 1 evening

2. BINARY NOTATION (1=take, 0=skip when mixed):
   - '1-0-0' = Once daily morning
   - '1-1-0' = Twice daily
   - '1-1-1' = Three times daily

3. MEDICAL ABBREVIATIONS:
   - OD = Once Daily, BD = Twice Daily
   - TDS/TID = Three times daily, QDS/QID = Four times daily
   - AC = Before meals, PC = After meals, HS = At bedtime
   - SOS/PRN = As needed (frequencyCount: 0)

JSON Format:
{{
  ""patient"": {{""name"": ""string"", ""age"": number, ""gender"": ""string""}},
  ""doctor"": {{""name"": ""string"", ""registration_number"": ""string"", ""specialization"": ""string""}},
  ""prescription_date"": ""YYYY-MM-DD"",
  ""medications"": [
    {{
      ""name"": ""string (required)"",
      ""dosage"": ""string"",
      ""unit"": ""string"",
      ""frequency"": ""string (e.g., 'Three times daily')"",
      ""frequencyCount"": number (1-4, or 0 for SOS),
      ""duration"": ""string"",
      ""durationDays"": number,
      ""timing"": ""string"",
      ""instructions"": ""string"",
      ""confidenceScore"": number (0.0-1.0)
    }}
  ]
}}

Prescription Text:
{ocrText}

CRITICAL RULES:

1. DATE EXTRACTION (HIGHEST PRIORITY):
   - Look for ""Date:"", ""Dated:"", or date patterns
   - Found: ""28/1/26"" ? Return: ""2026-01-28""
   - Found: ""28-01-2025"" ? Return: ""2025-01-28""  
   - ALWAYS convert to YYYY-MM-DD format
   - If year is 2 digits (26), assume 20XX (2026)

2. MEDICATION EXTRACTION:
   - Extract ALL medications
   - '0 0 0' = Three times daily (NOT skip!)
   - Convert duration to days (1 week = 7, 1 month = 30)
   - SOS/PRN medications: set frequencyCount to 0, durationDays to null

3. OUTPUT:
   - Use null for missing fields
   - Return valid JSON only (no markdown)

EXAMPLE: Input ""Date: 28/1/26"" ? Output ""prescription_date"": ""2026-01-28""";
    }

    private PrescriptionReadResult ParseStructuredData(string jsonContent)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Claude Parser: Parsing structured JSON...");

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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<PrescriptionReadResult>(jsonContent, options);

            if (data == null)
            {
                return new PrescriptionReadResult
                {
                    Success = false,
                    Medications = new List<MedicationData>()
                };
            }

            data.Success = true;
            return data;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Claude Parser: JSON parsing error: {ex.Message}");
            return new PrescriptionReadResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
    }

    private bool IsRetryableError(Exception ex)
    {
        return ex is HttpRequestException || 
               ex is TaskCanceledException ||
               (ex is InvalidOperationException && ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }

    // Claude API Response models
    private class ClaudeApiResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("content")]
        public List<ClaudeContent>? Content { get; set; }
    }

    private class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

