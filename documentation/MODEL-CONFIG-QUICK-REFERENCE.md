# Quick Reference: AI Model Configuration

## ? Changes Made

### 1. Configuration Structure
```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-...",
    "Model": "gpt-4o",        // ? Now configurable
    "TimeoutSeconds": 30,
    "MaxTokens": 1000
  }
}
```

### 2. Service Updates

#### OpenAIPrescriptionReaderService
**Before**:
```csharp
// Hardcoded model
var request = new { model = "gpt-4o", ... };
```

**After**:
```csharp
private readonly string _modelName; // From configuration

public OpenAIPrescriptionReaderService(..., string modelName = "gpt-4o")
{
    _modelName = modelName;
}

// Uses configured model
var request = new { model = _modelName, ... };
```

#### MauiProgram.cs
**Before**:
```csharp
var chatClient = new ChatClient("gpt-4o", openAIKey);  // Hardcoded
```

**After**:
```csharp
var openAIModel = config.OpenAI.Model;  // From config
var chatClient = new ChatClient(openAIModel, openAIKey);
```

#### Program.cs (API)
**Before**:
```csharp
var chatClient = new ChatClient("gpt-4o", openAIKey);  // Hardcoded
```

**After**:
```csharp
var openAIModel = config["OpenAI:Model"] ?? "gpt-4o";
var chatClient = new ChatClient(openAIModel, openAIKey);
```

## ?? Current Configuration

### Development Environment
```json
{
  "Development": {
    "OpenAI": {
      "Model": "gpt-4o"  // Full GPT-4 Omni
    }
  }
}
```

### Staging Environment
```json
{
  "Staging": {
    "OpenAI": {
      "Model": "gpt-4o"  // Same as production for testing
    }
  }
}
```

### Production Environment
```json
{
  "Production": {
    "OpenAI": {
      "Model": "gpt-4o-mini"  // Cost-optimized (60% cheaper)
    }
  }
}
```

## ?? Usage Locations

### 1. MedicalPrescriptionParserAgent
- **Uses**: ChatClient with configured model
- **File**: `backend/MedRemind.Services/AI/MedicalPrescriptionParserAgent.cs`
- **Purpose**: Text-based prescription parsing

### 2. OpenAIPrescriptionReaderService
- **Uses**: Model name for Vision API fallback
- **File**: `backend/MedRemind.Services/AI/OpenAIPrescriptionReaderService.cs`
- **Purpose**: Fallback image processing

### 3. Semantic Kernel Multi-Agent System
- **Uses**: Configured model for all 3 agents
- **Files**: `backend/MedRemind.Services/AI/Agents/*`
- **Purpose**: Advanced extraction with validation

## ?? How to Change Model

### For Testing
**Edit**: `mobile/MedRemind.Mobile/appsettings.json`

```json
{
  "ActiveEnvironment": "Development",
  "Environments": {
    "Development": {
      "OpenAI": {
        "Model": "gpt-3.5-turbo"  // ? Change here for testing
      }
    }
  }
}
```

### For Production
**Edit**: `backend/MedRemind.API/appsettings.json`

```json
{
  "OpenAI": {
    "Model": "gpt-4o-mini"  // ? Recommended for production
  }
}
```

## ?? Cost Comparison

| Model | Cost (per 1M tokens) | Use Case |
|-------|---------------------|----------|
| gpt-4o | $0.25 | Development |
| gpt-4o-mini | $0.15 | Production (60% cheaper) |
| gpt-3.5-turbo | $0.05 | Budget testing |

## ? Verification

### Check Configuration Loaded
```bash
# Run app and check logs
? OpenAI API configured
   Environment: Development
   Model: gpt-4o
   ChatClient initialized
```

### Test Prescription Processing
1. Upload prescription image
2. Check debug logs for model name
3. Verify processing works correctly

## ?? Troubleshooting

### Model Not Applied
**Symptom**: Still using hardcoded model

**Fix**:
1. Verify `appsettings.json` has `Model` field
2. Rebuild solution: `dotnet build`
3. Check service registration passes model parameter

### Model Not Found Error
**Symptom**: `The model 'gpt-4o' does not exist`

**Fix**:
1. Verify API key has access to model
2. Check OpenAI dashboard for available models
3. Use fallback model: `gpt-4o-mini` or `gpt-3.5-turbo`

## ?? Model Selection Guide

```
Fast & Cheap
     ?
???????????????????
? gpt-3.5-turbo  ? Budget testing only
???????????????????
? gpt-4o-mini    ? ? PRODUCTION (recommended)
???????????????????
? gpt-4o         ? ? DEVELOPMENT
???????????????????
? gpt-4-turbo    ? High accuracy (expensive)
???????????????????
     ?
Accurate & Expensive
```

## ?? Recommended Configuration

### For New Projects
```json
{
  "ActiveEnvironment": "Development",
  "Environments": {
    "Development": {
      "OpenAI": { "Model": "gpt-4o" }
    },
    "Production": {
      "OpenAI": { "Model": "gpt-4o-mini" }
    }
  }
}
```

### For Cost Optimization
```json
{
  "Production": {
    "OpenAI": {
      "Model": "gpt-4o-mini",  // 60% cheaper
      "MaxTokens": 1000         // Limit tokens
    }
  }
}
```

## ?? Next Steps

1. ? Model configuration complete
2. ? Fix remaining build errors (ViewModel type mismatches)
3. ? Test with different models
4. ? Monitor costs in production

---

**Quick Reference Version**: 1.0  
**Last Updated**: December 26, 2024  
**Status**: Configuration ? | Testing Pending ?
