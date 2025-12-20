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

    public OpenAIPrescriptionReaderService(
        HttpClient httpClient, 
        string apiKey,
        IValidationAgentService validationAgent)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _validationAgent = validationAgent;
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
            var request = CreateOpenAIRequest(base64Image);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API error: {error}"
                };
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse);

            if (openAiResponse?.Choices == null || openAiResponse.Choices.Count == 0)
            {
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "No response from OpenAI"
                };
            }

            var content = openAiResponse.Choices[0].Message.Content;
            var result = ParsePrescriptionData(content);

            // Validate medications
            if (result.Success && result.Medications.Any())
            {
                var warnings = await _validationAgent.ValidateMedicationsAsync(
                    result.Medications, 
                    cancellationToken);
                result.Warnings = warnings;

                // Calculate overall confidence score
                result.ConfidenceScore = CalculateOverallConfidence(result.Medications);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error processing prescription: {ex.Message}"
            };
        }
    }

    private object CreateOpenAIRequest(string base64Image)
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
            model = "gpt-4o",
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

    private PrescriptionReadResult ParsePrescriptionData(string jsonContent)
    {
        try
        {
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

            var data = JsonSerializer.Deserialize<PrescriptionJsonData>(jsonContent, options);

            if (data == null)
            {
                return new PrescriptionReadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse prescription data"
                };
            }

            return new PrescriptionReadResult
            {
                Success = true,
                DoctorName = data.DoctorName,
                PrescriptionDate = ParseDate(data.PrescriptionDate),
                Medications = data.Medications ?? new List<MedicationData>(),
                ConfidenceScore = 0.0
            };
        }
        catch (Exception ex)
        {
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = $"Error parsing prescription data: {ex.Message}"
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

    private double CalculateOverallConfidence(List<MedicationData> medications)
    {
        if (!medications.Any())
            return 0.0;

        return medications.Average(m => m.ConfidenceScore);
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

    private class PrescriptionJsonData
    {
        public string? DoctorName { get; set; }
        public string? PrescriptionDate { get; set; }
        public List<MedicationData>? Medications { get; set; }
    }
}
