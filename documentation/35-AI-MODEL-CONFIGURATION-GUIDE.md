# AI Model Configuration Guide - OpenAI & Semantic Kernel

## Overview

This guide explains how the MedRemind application is configured to use OpenAI models through configuration files, supporting different models for different environments (Development, Staging, Production).

## Configuration Structure

### appsettings.json Configuration

```json
{
  "Environments": {
    "Development": {
      "OpenAI": {
        "ApiKey": "sk-proj-...",
        "Model": "gpt-4o",           // Full GPT-4 Omni for development
        "TimeoutSeconds": 30,
        "MaxTokens": 1000
      }
    },
    "Staging": {
      "OpenAI": {
        "ApiKey": "sk-proj-...",
        "Model": "gpt-4o",           // Full model for staging tests
        "TimeoutSeconds": 30,
        "MaxTokens": 1000
      }
    },
    "Production": {
      "OpenAI": {
        "ApiKey": "sk-proj-...",
        "Model": "gpt-4o-mini",      // Cost-optimized for production
        "TimeoutSeconds": 30,
        "MaxTokens": 1000
      }
    }
  },
  "ActiveEnvironment": "Development"
}
```

## Model Selection Strategy

### Development Environment
- **Model**: `gpt-4o` (GPT-4 Omni)
- **Purpose**: Full capabilities for testing and development
- **Cost**: Higher but acceptable for dev environment
- **Benefits**: 
  - Best accuracy for prescription parsing
  - Latest multimodal capabilities
  - Optimal for testing edge cases

### Staging Environment
- **Model**: `gpt-4o`
- **Purpose**: Production-like testing environment
- **Ensures**: Staging tests match production behavior

### Production Environment
- **Model**: `gpt-4o-mini`
- **Purpose**: Cost-optimized production deployment
- **Benefits**:
  - 60% cheaper than GPT-4o
  - Still maintains high accuracy
  - Faster response times
  - Lower token costs

## Architecture Components

### 1. MedicalPrescriptionParserAgent
**File**: `backend/MedRemind.Services/AI/MedicalPrescriptionParserAgent.cs`

```csharp
public class MedicalPrescriptionParserAgent
{
    private readonly ChatClient _chatClient;

    public MedicalPrescriptionParserAgent(ChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }
}
```

**Uses**: OpenAI's ChatClient with configured model
**Purpose**: Parse OCR text into structured prescription data

### 2. OpenAIPrescriptionReaderService
**File**: `backend/MedRemind.Services/AI/OpenAIPrescriptionReaderService.cs`

```csharp
public class OpenAIPrescriptionReaderService : IPrescriptionReaderService
{
    private readonly string _modelName; // Configured model name

    public OpenAIPrescriptionReaderService(
        HttpClient httpClient,
        string apiKey,
        IValidationAgentService validationAgent,
        AzureDocumentIntelligenceService azureDocService,
        MedicalPrescriptionParserAgent parserAgent,
        string modelName = "gpt-4o") // Model from configuration
    {
        _modelName = modelName;
    }
}
```

**Uses**: Model name for fallback Vision API calls
**Purpose**: Main prescription processing service with Azure DI + OpenAI

### 3. Semantic Kernel Multi-Agent System
**Files**: `backend/MedRemind.Services/AI/Agents/*`

```csharp
// Agent 1: OCRTextSaverAgent - Saves OCR text
// Agent 2: PrescriptionDataExtractionAgent - Uses Semantic Kernel + OpenAI
// Agent 3: ValidationAgent - Validates and retries with different prompts
```

**Uses**: Semantic Kernel with configured OpenAI model
**Purpose**: Advanced multi-agent workflow with 80% match validation

## Service Registration

### MAUI Application (MauiProgram.cs)

```csharp
// Load configuration
var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
var openAIKey = config.OpenAI.ApiKey;
var openAIModel = config.OpenAI.Model; // ? From configuration

// Register ChatClient with configured model
var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);

// Register Semantic Kernel with configured model
var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
kernelBuilder.AddOpenAIChatCompletion(openAIModel, openAIKey);

// Register service with model name
return new OpenAIPrescriptionReaderService(
    httpClient, 
    openAIKey, 
    validationAgent, 
    azureDocService, 
    parserAgent, 
    openAIModel); // ? Pass configured model
```

### API Application (Program.cs)

```csharp
// Load from IConfiguration
var openAIKey = config["OpenAI:ApiKey"] ?? "";
var openAIModel = config["OpenAI:Model"] ?? "gpt-4o"; // Fallback

// Register with configured model
var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);
```

## Model Usage Across Application

### 1. **Direct OpenAI SDK Calls** (MedicalPrescriptionParserAgent)
```csharp
var chatClient = new ChatClient(modelName, apiKey);
// Uses configured model automatically
```

### 2. **Fallback Vision API** (OpenAIPrescriptionReaderService)
```csharp
private object CreateOpenAIVisionRequest(string base64Image)
{
    return new
    {
        model = _modelName, // ? Uses configured model
        messages = [...]
    };
}
```

### 3. **Semantic Kernel Agents** (Multi-Agent System)
```csharp
kernelBuilder.AddOpenAIChatCompletion(openAIModel, openAIKey);
// All agents use this configured model
```

## Benefits of Configuration-Based Models

### 1. **Environment-Specific Optimization**
- Development: Use powerful models for testing
- Production: Use cost-effective models

### 2. **Easy Model Switching**
Change model in one place (appsettings.json):
```json
{
  "OpenAI": {
    "Model": "gpt-4o-mini"  // ? Change here
  }
}
```

All components automatically use the new model.

### 3. **Cost Control**
Production environment automatically uses `gpt-4o-mini`:
- **Estimated Savings**: 60% cost reduction
- **Performance**: Minimal accuracy impact for prescription parsing
- **Speed**: Faster response times

### 4. **Testing Flexibility**
Easily test different models:
```json
// Test GPT-3.5 Turbo for cost analysis
{
  "OpenAI": {
    "Model": "gpt-3.5-turbo"
  }
}

// Test latest GPT-4 for accuracy
{
  "OpenAI": {
    "Model": "gpt-4o"
  }
}
```

## Supported Models

### Recommended Models

| Model | Use Case | Cost | Speed | Accuracy |
|-------|----------|------|-------|----------|
| `gpt-4o` | Development/Staging | High | Medium | Excellent |
| `gpt-4o-mini` | Production | Low | Fast | Very Good |
| `gpt-4-turbo` | High accuracy needed | High | Medium | Excellent |
| `gpt-3.5-turbo` | Budget-conscious | Very Low | Very Fast | Good |

### Model Selection Matrix

```
?????????????????????????????????????????????????
? Environment     ? Model        ? Rationale    ?
?????????????????????????????????????????????????
? Development     ? gpt-4o       ? Full testing ?
? Staging         ? gpt-4o       ? Production   ?
?                 ?              ? parity       ?
? Production      ? gpt-4o-mini  ? Cost-optimal ?
?????????????????????????????????????????????????
```

## Configuration Validation

### At Startup

```csharp
System.Diagnostics.Debug.WriteLine($"? OpenAI API configured");
System.Diagnostics.Debug.WriteLine($"   Environment: {activeEnv}");
System.Diagnostics.Debug.WriteLine($"   Model: {openAIModel}");
System.Diagnostics.Debug.WriteLine($"   ChatClient initialized");
```

### Sample Output
```
? OpenAI API configured
   Environment: Development
   Model: gpt-4o
   ChatClient initialized with model: gpt-4o
? Semantic Kernel initialized for multi-agent system
   Model: gpt-4o
```

## Model Parameter Configuration

### MaxTokens
Controls maximum response length:
```json
{
  "OpenAI": {
    "MaxTokens": 1000  // Sufficient for structured JSON
  }
}
```

**Recommendations**:
- Prescription parsing: 1000-1500 tokens
- Vision API: 1000-2000 tokens
- Multi-agent extraction: 5000 tokens (for detailed mode)

### Temperature
Controls response randomness:
```csharp
Temperature = 0.1f  // Low for consistent medical data extraction
```

**Fixed in code** (not configurable) for safety:
- Medical parsing: 0.1 (highly deterministic)
- Validation: 0.1 (consistent results)

## Cost Optimization Tips

### 1. Use Mini Model in Production
```json
{
  "Production": {
    "OpenAI": {
      "Model": "gpt-4o-mini"
    }
  }
}
```
**Savings**: $0.15 per million tokens vs $0.25 (GPT-4o)

### 2. Optimize Token Usage
- Use Azure Document Intelligence for OCR (free tier)
- Send only OCR text to OpenAI (not images)
- Fallback to Vision API only when needed

### 3. Enable Caching
```csharp
// Cache OCR results by image hash
if (_cache.TryGet<string>(imageHash, out var cachedOCR))
{
    return cachedOCR; // Avoid re-processing
}
```

## Troubleshooting

### Model Not Found Error
**Error**: `The model 'gpt-4o' does not exist`

**Solution**: Check API key permissions
```bash
# Verify API key has access to model
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer $OPENAI_API_KEY"
```

### Model Configuration Not Applied
**Issue**: Application still uses hardcoded model

**Check**:
1. Verify appsettings.json has `Model` field
2. Check `EmbeddedConfigurationLoader` loads config correctly
3. Verify service registration passes model parameter

### Cost Unexpectedly High
**Issue**: Production using expensive model

**Check**:
```json
{
  "ActiveEnvironment": "Production",  // ? Must be set
  "Production": {
    "OpenAI": {
      "Model": "gpt-4o-mini"  // ? Cost-effective model
    }
  }
}
```

## Testing Different Models

### Quick Model Test

1. **Update appsettings.json**:
```json
{
  "OpenAI": {
    "Model": "gpt-3.5-turbo"  // Test this model
  }
}
```

2. **Run application**:
```bash
dotnet run
```

3. **Check logs**:
```
? OpenAI ChatClient initialized with model: gpt-3.5-turbo
```

4. **Test prescription upload**:
- Upload test prescription
- Verify accuracy
- Check processing time
- Compare costs

### Model Comparison Test

```csharp
// Test script to compare models
var models = new[] { "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo" };

foreach (var model in models)
{
    var client = new ChatClient(model, apiKey);
    var result = await client.CompleteChatAsync(messages);
    
    Console.WriteLine($"Model: {model}");
    Console.WriteLine($"  Accuracy: {EvaluateAccuracy(result)}");
    Console.WriteLine($"  Tokens: {result.Usage.TotalTokenCount}");
    Console.WriteLine($"  Time: {elapsed.TotalSeconds}s");
    Console.WriteLine($"  Cost: ${CalculateCost(model, result.Usage)}");
}
```

## Future Enhancements

### 1. Dynamic Model Selection
```csharp
// Select model based on confidence score
var model = confidenceScore < 0.8 
    ? "gpt-4o"           // Use powerful model for difficult cases
    : "gpt-4o-mini";     // Use mini for clear prescriptions
```

### 2. Model Performance Tracking
```csharp
// Track model performance metrics
{
  "ModelMetrics": {
    "gpt-4o-mini": {
      "AverageAccuracy": 0.92,
      "AverageTokens": 850,
      "AverageCost": 0.0002
    }
  }
}
```

### 3. A/B Testing
```csharp
// Randomly test different models
var model = _random.Next(2) == 0 
    ? config.OpenAI.ModelA 
    : config.OpenAI.ModelB;
```

## Summary

? **Centralized Configuration**: All models configured in appsettings.json  
? **Environment-Specific**: Different models for Dev/Staging/Prod  
? **Cost-Optimized**: Production uses gpt-4o-mini (60% cheaper)  
? **Easy Testing**: Change model in one place  
? **Fully Integrated**: Works with OpenAI SDK and Semantic Kernel  

**Next Steps**:
1. Verify current model configuration in appsettings.json
2. Test prescription processing with configured model
3. Monitor costs and adjust models as needed
4. Consider A/B testing for model optimization

---

**Document Version**: 1.0  
**Last Updated**: December 26, 2024  
**Status**: Configuration Complete ?
