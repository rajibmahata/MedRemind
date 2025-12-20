# API Integration Guide

## Overview

MedRemind integrates with two primary external APIs:
1. **OpenAI GPT-4 Vision API** - For prescription reading
2. **2Factor.in API** - For SMS OTP delivery

**Document Version:** 1.0  
**Last Updated:** December 20, 2025

---

## Table of Contents

1. [OpenAI Vision API](#openai-vision-api)
2. [2Factor.in SMS API](#2factorin-sms-api)
3. [Error Handling](#error-handling)
4. [Rate Limiting](#rate-limiting)
5. [Security Best Practices](#security-best-practices)
6. [Testing](#testing)

---

## OpenAI Vision API

### Setup

1. Create account at https://platform.openai.com
2. Generate API key
3. Add to configuration:

```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4-vision-preview",
    "MaxTokens": 1000,
    "Temperature": 0.2
  }
}
```

### Implementation

```csharp
public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string API_URL = "https://api.openai.com/v1/chat/completions";
    
    public async Task<PrescriptionData> ProcessPrescriptionAsync(string imagePath)
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);
        
        var requestBody = new
        {
            model = "gpt-4-vision-preview",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = GetPrescriptionPrompt() },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
            },
            max_tokens = 1000,
            temperature = 0.2
        };
        
        var response = await _httpClient.PostAsJsonAsync(API_URL, requestBody);
        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
        
        return ParsePrescriptionData(result.Choices[0].Message.Content);
    }
    
    private string GetPrescriptionPrompt()
    {
        return @"Analyze this prescription image and extract medication information in JSON format:
        {
          ""doctor"": ""Doctor name"",
          ""hospital"": ""Hospital name"",
          ""date"": ""YYYY-MM-DD"",
          ""medications"": [
            {
              ""name"": ""Medicine name"",
              ""dosage"": ""500mg"",
              ""frequency"": ""twice daily"",
              ""duration"": ""7 days"",
              ""instructions"": ""After meals"",
              ""confidence"": ""HIGH/MEDIUM/LOW""
            }
          ]
        }
        
        If any field is unclear, mark confidence as LOW.";
    }
}
```

### Cost Optimization

- **Image compression**: Resize images to max 2048px before sending
- **Caching**: Cache results for identical images
- **Batch processing**: Process multiple prescriptions if possible
- **Use GPT-4 Mini**: Consider using cheaper model for simple prescriptions

### Expected Costs

| Usage | Cost per Request | Monthly (100 users) |
|-------|------------------|---------------------|
| Standard | $0.01-0.03 | $50-150 |
| Optimized | $0.005-0.015 | $25-75 |

---

## 2Factor.in SMS API

### Setup

See [04-2factor-sms-integration.md](04-2factor-sms-integration.md) for complete setup guide.

### Quick Start

```csharp
public class TwoFactorService
{
    private readonly string _apiKey;
    private const string BASE_URL = "https://2factor.in/API/V1";
    
    public async Task<OTPResponse> SendOTPAsync(string phoneNumber)
    {
        var otp = GenerateOTP();
        phoneNumber = CleanPhoneNumber(phoneNumber);
        
        var url = $"{BASE_URL}/{_apiKey}/SMS/{phoneNumber}/{otp}/MEDRMD";
        var response = await _httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<TwoFactorResponse>();
            return new OTPResponse
            {
                IsSuccess = true,
                SessionId = result.Details,
                OTP = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };
        }
        
        return new OTPResponse { IsSuccess = false };
    }
    
    public async Task<bool> VerifyOTPAsync(string sessionId, string otp)
    {
        var url = $"{BASE_URL}/{_apiKey}/SMS/VERIFY/{sessionId}/{otp}";
        var response = await _httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<TwoFactorResponse>();
            return result.Status == "Success";
        }
        
        return false;
    }
}
```

---

## Error Handling

### Retry Strategy

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> action, 
    int maxRetries = 3)
{
    int attempt = 0;
    
    while (attempt < maxRetries)
    {
        try
        {
            return await action();
        }
        catch (HttpRequestException ex) when (attempt < maxRetries - 1)
        {
            attempt++;
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    
    throw new Exception($"Failed after {maxRetries} attempts");
}
```

### Error Codes

| API | Error Code | Meaning | Action |
|-----|------------|---------|--------|
| OpenAI | 401 | Invalid API key | Check configuration |
| OpenAI | 429 | Rate limit exceeded | Implement backoff |
| OpenAI | 500 | Server error | Retry with backoff |
| 2Factor | 403 | Insufficient balance | Recharge account |
| 2Factor | 404 | Invalid phone | Validate input |

---

## Rate Limiting

### OpenAI Limits

- **Tier 1**: 500 requests/day
- **Tier 2**: 3,500 requests/day
- **Implement**: Request queuing and throttling

### 2Factor.in Limits

- **Default**: No hard limit
- **Recommended**: Max 3 OTP per phone per hour
- **Implement**: User-level rate limiting

```csharp
public class RateLimitService
{
    private readonly Dictionary<string, List<DateTime>> _requestLog = new();
    
    public bool IsAllowed(string key, int maxRequests, TimeSpan window)
    {
        if (!_requestLog.ContainsKey(key))
        {
            _requestLog[key] = new List<DateTime>();
        }
        
        var now = DateTime.UtcNow;
        var windowStart = now.Subtract(window);
        
        _requestLog[key] = _requestLog[key]
            .Where(t => t > windowStart)
            .ToList();
        
        if (_requestLog[key].Count >= maxRequests)
        {
            return false;
        }
        
        _requestLog[key].Add(now);
        return true;
    }
}
```

---

## Security Best Practices

1. **Never commit API keys** - Use environment variables
2. **Encrypt API keys** - In configuration files
3. **Validate inputs** - Before API calls
4. **Log without sensitive data** - Don't log API keys, OTPs
5. **Use HTTPS only** - For all API calls
6. **Implement timeouts** - Prevent hanging requests
7. **Monitor usage** - Track API costs

### Configuration Management

```csharp
// Use user secrets in development
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "TwoFactor:ApiKey" "your-key"

// Use environment variables in production
export OPENAI_API_KEY="sk-..."
export TWOFACTOR_API_KEY="your-key"
```

---

## Testing

### Mock Services for Testing

```csharp
public class MockOpenAIService : IOpenAIService
{
    public Task<PrescriptionData> ProcessPrescriptionAsync(string imagePath)
    {
        return Task.FromResult(new PrescriptionData
        {
            DoctorName = "Dr. Test",
            Medications = new List<MedicationData>
            {
                new MedicationData
                {
                    Name = "Amoxicillin",
                    Dosage = "500mg",
                    Frequency = "Twice daily",
                    Duration = "7 days",
                    Confidence = ConfidenceLevel.High
                }
            }
        });
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class APIIntegrationTests
{
    [TestMethod]
    public async Task OpenAI_ProcessPrescription_ReturnsValidData()
    {
        // Arrange
        var service = new OpenAIService(configuration);
        var testImage = "test_prescription.jpg";
        
        // Act
        var result = await service.ProcessPrescriptionAsync(testImage);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Medications.Count > 0);
    }
}
```

---

## Monitoring

### Key Metrics

1. **API Response Time**: Target < 3 seconds
2. **Success Rate**: Target > 95%
3. **Error Rate**: Target < 5%
4. **Cost per Request**: Monitor daily

### Logging

```csharp
public class APILogger
{
    public void LogAPICall(string apiName, string endpoint, TimeSpan duration, bool success)
    {
        _logger.LogInformation(
            "API Call: {ApiName} | Endpoint: {Endpoint} | Duration: {Duration}ms | Success: {Success}",
            apiName,
            endpoint,
            duration.TotalMilliseconds,
            success
        );
    }
}
```

---

**End of API Integration Guide**

[← Back to Timeline](05-project-timeline.md) | [Next: Team Assignment →](07-team-assignment.md)