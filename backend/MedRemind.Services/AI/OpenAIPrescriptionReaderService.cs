using System.Net.Http.Json;
using System.Text.Json;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;

namespace MedRemind.Services.AI;

public class OpenAIPrescriptionReaderService : IPrescriptionReaderService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IValidationAgentService _validationAgent;
    private readonly AzureDocumentIntelligenceService _azureDocService;
    private readonly OpenAIPrescriptionParserAgent _parserAgent;
    private readonly string _modelName; // Store model name

    public OpenAIPrescriptionReaderService(
        HttpClient httpClient,
        string apiKey,
        IValidationAgentService validationAgent,
        AzureDocumentIntelligenceService azureDocService,
        OpenAIPrescriptionParserAgent parserAgent,
        string modelName = "gpt-4o") // Add model parameter with default
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _validationAgent = validationAgent;
        _azureDocService = azureDocService;
        _parserAgent = parserAgent;
        _modelName = modelName;
        
        System.Diagnostics.Debug.WriteLine($"✅ PrescriptionReaderService initialized with model: {_modelName}");
    }

    public async Task<PrescriptionReadResult> ReadPrescriptionAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Read image and convert to base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);

            return await ReadPrescriptionFromBase64Async(base64Image, cancellationToken);
        }
        catch (Exception ex)
        {
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error reading prescription: {ex.Message}"
            };
        }
    }

    public async Task<PrescriptionReadResult> ReadPrescriptionFromBase64Async(
        string base64Image,
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine(" Prescription Processing: Starting...");
            System.Diagnostics.Debug.WriteLine($"   Image size: {base64Image?.Length ?? 0} bytes");

            if (string.IsNullOrEmpty(base64Image))
            {
                System.Diagnostics.Debug.WriteLine("? Image is empty!");
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "Image data is empty"
                };
            }

            // Step 1: Extract text using Azure Document Intelligence
            System.Diagnostics.Debug.WriteLine("?? Step 1: Extracting text with Azure Document Intelligence...");
            string extractedText;

            try
            {
                extractedText = await _azureDocService.ExtractTextFromImageAsync(base64Image, cancellationToken);
                System.Diagnostics.Debug.WriteLine($"? Text extracted: {extractedText.Length} characters");
                System.Diagnostics.Debug.WriteLine($"   Preview: {extractedText.Substring(0, Math.Min(200, extractedText.Length))}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Azure DI failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("?? Fallback: Using OpenAI Vision API...");

                // Fallback to OpenAI Vision if Azure DI fails
                return await ProcessWithOpenAIVisionAsync(base64Image, cancellationToken);
            }

            // Step 2: Send extracted text to Parser Agent for structured extraction
            System.Diagnostics.Debug.WriteLine("?? Step 2: Processing text with Medical Parser Agent...");

            if (string.IsNullOrEmpty(_apiKey))
            {
                System.Diagnostics.Debug.WriteLine("? OpenAI: API key is empty!");
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key is not configured"
                };
            }

            try
            {
                var parseResult = await _parserAgent.ParsePrescriptionTextAsync(extractedText, cancellationToken);

                System.Diagnostics.Debug.WriteLine($"? Parser Agent: Success");
                System.Diagnostics.Debug.WriteLine($"   Patient: {parseResult.Patient?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Doctor: {parseResult.Doctor?.Name ?? "N/A"}");
                System.Diagnostics.Debug.WriteLine($"   Medications: {parseResult.Medications.Count}");

                // Create result from parsed data
                var result = new PrescriptionReadResult
                {
                    Success = true,
                    Doctor = parseResult.Doctor,
                    PrescriptionDate = parseResult.PrescriptionDate,
                    Medications = parseResult.Medications,
                    ConfidenceScore = 0.0
                };

                if (parseResult.Medications.Any())
                {
                    foreach (var med in parseResult.Medications)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {med.Name}: {med.Dosage} {med.Unit}, {med.Frequency}");
                    }

                    // Validate medications
                    System.Diagnostics.Debug.WriteLine("?? Validation: Starting medication validation...");

                    var warnings = await _validationAgent.ValidateMedicationsAsync(
                        result.Medications,
                        cancellationToken);

                    result.ValidationWarnings = warnings;
                    System.Diagnostics.Debug.WriteLine($"? Validation: Complete. Warnings: {warnings.Count}");

                    // Calculate overall confidence score
                    result.ConfidenceScore = CalculateOverallConfidence(result.Medications);
                    System.Diagnostics.Debug.WriteLine($"? Confidence Score: {result.ConfidenceScore:P0}");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Parser Agent failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("?? Fallback: Using OpenAI Vision API...");

                // Fallback to OpenAI Vision if parser fails
                return await ProcessWithOpenAIVisionAsync(base64Image, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? HTTP error: {ex.Message}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Timeout: {ex.Message}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = "Request timed out. Please try again."
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Unexpected error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error processing prescription: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Fallback method using OpenAI Vision API (more expensive but works without Azure DI)
    /// </summary>
    private async Task<PrescriptionReadResult> ProcessWithOpenAIVisionAsync(
        string base64Image,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = CreateOpenAIVisionRequest(base64Image);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                request,
                cancellationToken);

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = $"OpenAI Vision API error ({response.StatusCode})"
                };
            }

            var openAiResponse = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse);
            if (openAiResponse?.Choices == null || openAiResponse.Choices.Count == 0)
            {
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "No response from OpenAI Vision"
                };
            }

            var content = openAiResponse.Choices[0].Message.Content;
            return ParsePrescriptionData(content);
        }
        catch (Exception ex)
        {
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"OpenAI Vision fallback failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Create request for text-based extraction (cheaper and faster)
    /// </summary>
    private object CreateOpenAITextRequest(string extractedText)
    {
        var prompt = $@"You are a medical prescription data extraction AI. Analyze the following prescription text and extract structured medication information.

Prescription Text:
{extractedText}

Return ONLY a valid JSON object (no markdown, no code blocks) with this exact structure:
{{
  ""doctorName"": ""Dr. Name"",
  ""prescriptionDate"": ""YYYY-MM-DD"",
  ""medications"": [
    {{
      ""name"": ""Medication name"",
      ""dosage"": ""Amount"",
      ""unit"": ""Tablet/Capsule/ml/mg/drops/puffs"",
      ""frequency"": ""Once daily/Twice daily/Three times daily/Four times daily"",
      ""frequencyCount"": 1,
      ""durationDays"": 7,
      ""instructions"": ""Take with food"",
      ""confidenceScore"": 0.95
    }}
  ]
}}

Rules:
- Extract all medications from the text
- Use standard frequency terms
- Set confidenceScore (0.0-1.0) based on clarity
- If unsure about any field, use best guess but lower confidence
- Return empty medications array if no medications found";

        return new
        {
            model = _modelName, // Use configured model
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            max_tokens = 1000,
            temperature = 0.2
        };
    }

    /// <summary>
    /// Create request for vision-based extraction (fallback)
    /// </summary>
    private object CreateOpenAIVisionRequest(string base64Image)
    {
        var prompt = @"You are a medical prescription reader AI. Analyze this prescription image and extract medication information.

Return ONLY a valid JSON object (no markdown, no code blocks) with this exact structure:
{
  ""doctorName"": ""Dr. Name"",
  ""prescriptionDate"": ""YYYY-MM-DD"",
  ""medications"": [
    {
      ""name"": ""Medication name"",
      ""dosage"": ""Amount"",
      ""unit"": ""Tablet/Capsule/ml/mg/drops/puffs"",
      ""frequency"": ""Once daily/Twice daily/Three times daily/Four times daily"",
      ""frequencyCount"": 1,
      ""durationDays"": 7,
      ""instructions"": ""Take with food"",
      ""confidenceScore"": 0.95
    }
  ]
}

Rules:
- Extract all medications from the prescription
- Use standard frequency terms
- Set confidenceScore (0.0-1.0) based on image clarity
- If unsure about any field, use best guess but lower confidence
- Return empty medications array if no medications found";

        return new
        {
            model = _modelName, // Use configured model
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:image/jpeg;base64,{base64Image}"
                            }
                        }
                    }
                }
            },
            max_tokens = 1000,
            temperature = 0.2
        };
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
    }

    private double CalculateOverallConfidence(List<MedicationData> medications)
    {
        if (!medications.Any())
            return 0.0;

        return medications.Average(m =>(double) m.ConfidenceScore);
    }

    private PrescriptionReadResult ParsePrescriptionData(string jsonContent)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("?? Parsing: Starting JSON parsing...");
            System.Diagnostics.Debug.WriteLine($"   Raw content length: {jsonContent.Length}");

            // Remove markdown code blocks if present
            jsonContent = jsonContent.Trim();

            if (jsonContent.StartsWith("```json"))
            {
                System.Diagnostics.Debug.WriteLine("   Removing ```json wrapper");
                jsonContent = jsonContent.Substring(7);
            }
            if (jsonContent.StartsWith("```"))
            {
                System.Diagnostics.Debug.WriteLine("   Removing ``` wrapper");
                jsonContent = jsonContent.Substring(3);
            }
            if (jsonContent.EndsWith("```"))
            {
                System.Diagnostics.Debug.WriteLine("   Removing trailing ```");
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
            }
            jsonContent = jsonContent.Trim();

            System.Diagnostics.Debug.WriteLine($"   Cleaned content length: {jsonContent.Length}");
            System.Diagnostics.Debug.WriteLine($"   Content preview: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<PrescriptionReadResult>(jsonContent, options);

            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("? Parsing: Deserialization returned null");
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse prescription data - null result"
                };
            }

            System.Diagnostics.Debug.WriteLine($"? Parsing: Deserialization successful");
            System.Diagnostics.Debug.WriteLine($"   Doctor: {data.Doctor.Name ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Date: {(data.PrescriptionDate.HasValue ? data.PrescriptionDate.Value.ToString("dd/MM/yyyy") : "N/A")}");
            System.Diagnostics.Debug.WriteLine($"   Medications: {data.Medications?.Count ?? 0}");

            if (data.Medications != null && data.Medications.Any())
            {
                foreach (var med in data.Medications)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {med.Name}: {med.Dosage} {med.Unit}, {med.Frequency}");
                }
            }

            return new PrescriptionReadResult
            {
                Success = true,
                Doctor = data.Doctor,
                PrescriptionDate = data.PrescriptionDate,
                Medications = data.Medications ?? new List<MedicationData>(),
                ConfidenceScore = 0.0
            };
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Parsing: JSON error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Error at path: {ex.Path}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"JSON parsing error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Parsing: Unexpected error: {ex.Message}");
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error parsing prescription data: {ex.Message}"
            };
        }
    }

    // Helper classes for JSON deserialization
    private class OpenAIResponse
    {
        public List<Choice> Choices { get; set; } = new();
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }

    
}
