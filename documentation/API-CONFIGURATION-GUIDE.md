# ?? API Key Configuration Guide

## Overview

MedRemind uses **embedded configuration** for API keys. Keys are configured in `appsettings.json` file, embedded in the app binary at compile time, and encrypted at runtime.

**No UI configuration needed!** Users install the app and it works immediately.

---

## ?? Configuration File

**Location**: `mobile/MedRemind.Mobile/appsettings.json`

### **Structure**

```json
{
  "Environments": {
    "Development": {
      "OpenAI": {
        "ApiKey": "sk-proj-DEV_KEY_HERE",
        "Model": "gpt-4o",
        "TimeoutSeconds": 30,
        "MaxTokens": 1000
      },
      "TwoFactor": {
        "ApiKey": "2FACTOR_DEV_KEY_HERE",
        "TimeoutSeconds": 10
      },
      "Features": {
        "EnablePrescriptionUpload": true,
        "EnableVoiceReminders": true,
        "EnableAnalytics": false,
        "EnableCrashReporting": false
      }
    },
    "Staging": {
      "OpenAI": {
        "ApiKey": "sk-proj-STAGING_KEY_HERE",
        "Model": "gpt-4o",
        "TimeoutSeconds": 30,
        "MaxTokens": 1000
      },
      "TwoFactor": {
        "ApiKey": "2FACTOR_STAGING_KEY_HERE",
        "TimeoutSeconds": 10
      },
      "Features": {
        "EnablePrescriptionUpload": true,
        "EnableVoiceReminders": true,
        "EnableAnalytics": true,
        "EnableCrashReporting": true
      }
    },
    "Production": {
      "OpenAI": {
        "ApiKey": "sk-proj-PROD_KEY_HERE",
        "Model": "gpt-4o",
        "TimeoutSeconds": 30,
        "MaxTokens": 1000
      },
      "TwoFactor": {
        "ApiKey": "2FACTOR_PROD_KEY_HERE",
        "TimeoutSeconds": 10
      },
      "Features": {
        "EnablePrescriptionUpload": true,
        "EnableVoiceReminders": true,
        "EnableAnalytics": true,
        "EnableCrashReporting": true
      }
    }
  },
  "ActiveEnvironment": "Production"
}
```

---

## ?? API Keys

### **OpenAI API Key**

**Purpose**: AI-powered prescription scanning and medication validation

**Format**: `sk-proj-...` (starts with `sk-proj-` or `sk-`)

**Get Your Key**:
1. Go to https://platform.openai.com/api-keys
2. Sign up or log in
3. Click "Create new secret key"
4. Copy the key (starts with `sk-proj-...`)
5. Paste into `appsettings.json`

**Model Used**: `gpt-4o` (GPT-4 Optimized)

**Cost**: ~$0.01-0.05 per prescription scan

---

### **2Factor API Key**

**Purpose**: SMS OTP for authentication

**Get Your Key**:
1. Go to https://2factor.in
2. Sign up for an account
3. Get your API key from dashboard
4. Paste into `appsettings.json`

**Cost**: Per SMS sent (varies by region)

---

## ?? Setup Instructions

### **Step 1: Edit Configuration**

Open `mobile/MedRemind.Mobile/appsettings.json` and replace placeholders:

```json
{
  "ActiveEnvironment": "Production",
  "Environments": {
    "Production": {
      "OpenAI": {
        "ApiKey": "sk-proj-YOUR-ACTUAL-PRODUCTION-KEY-HERE"
      },
      "TwoFactor": {
        "ApiKey": "YOUR-2FACTOR-PRODUCTION-KEY-HERE"
      }
    }
  }
}
```

### **Step 2: Build App**

```bash
# For production
dotnet build -c Release

# For development
dotnet build -c Debug
```

### **Step 3: Deploy**

```
APK is generated with embedded, encrypted keys
Users install ? App works immediately!
NO USER CONFIGURATION NEEDED ?
```

---

## ?? Environment Management

### **Development Environment**

```json
"ActiveEnvironment": "Development"
```

**Use For**:
- Local testing
- Development builds
- Test API keys
- Debugging

**Build**:
```bash
dotnet build -c Debug
```

---

### **Staging Environment**

```json
"ActiveEnvironment": "Staging"
```

**Use For**:
- Pre-production testing
- UAT (User Acceptance Testing)
- Integration testing
- QA builds

**Build**:
```bash
dotnet build -c Release
```

---

### **Production Environment**

```json
"ActiveEnvironment": "Production"
```

**Use For**:
- Live users
- App store distribution
- Production deployment

**Build**:
```bash
dotnet build -c Release
```

**Sign APK** before distribution!

---

## ?? Security

### **How Keys Are Protected**

```
appsettings.json (plain text during development)
    ?
Embedded in app binary at compile time
    ?
Cannot extract as separate file from APK
    ?
Encrypted at runtime with AES-256
    ?
Device-specific encryption key
    ?
Keys never stored in plain text on device
    ?
Completely hidden from users
```

### **Security Features**

? **Embedded in Binary** - Not extractable as file  
? **Runtime Encryption** - AES-256 encryption  
? **Device-Specific** - Unique key per device  
? **Hidden from Users** - Completely invisible  
? **No UI Access** - Cannot view or modify  

---

## ?? Cost Management

### **OpenAI Costs**

| Environment | Monthly Cost | Usage |
|-------------|--------------|-------|
| **Development** | ~$5 | 100-500 test scans |
| **Staging** | ~$10-20 | 200-1000 test scans |
| **Production** | Variable | Based on real users |

**Per Prescription Scan**: $0.01 - $0.05

**Cost Control Tips**:
- Use separate OpenAI accounts per environment
- Set billing alerts in OpenAI dashboard
- Monitor usage regularly
- Use Development for testing (cheapest)

---

### **2Factor Costs**

| Region | Cost per SMS |
|--------|--------------|
| **India** | ~?0.20 ($0.0024) |
| **USA** | ~$0.01 |
| **Europe** | ~€0.01 |

**Cost varies by**:
- Destination country
- SMS volume
- 2Factor pricing plan

---

## ?? Key Rotation

### **When to Rotate**

- Every 90 days (recommended)
- If key is compromised
- When moving to production
- Security policy requires it

### **How to Rotate**

1. Generate new API key from OpenAI/2Factor
2. Update `appsettings.json` with new key
3. Rebuild app
4. Deploy updated app
5. Users get new keys automatically on update

**No user action required!**

---

## ?? Feature Flags

Control app features per environment:

```json
"Features": {
  "EnablePrescriptionUpload": true,    // AI prescription scanning
  "EnableVoiceReminders": true,        // Voice-based reminders
  "EnableAnalytics": false,            // Usage analytics
  "EnableCrashReporting": false        // Crash reporting
}
```

**Example Usage**:

```
Development:
  - All features enabled for testing
  
Staging:
  - Analytics enabled for pre-prod metrics
  - Crash reporting enabled
  
Production:
  - Only stable features enabled
  - Analytics and crash reporting enabled
```

---

## ?? Testing

### **Verify Configuration Loads**

Run app and check logs:

```
? Embedded configuration loaded - Active Environment: Production
? OpenAI API key loaded from embedded config for environment: Production
```

### **Test API Keys**

1. **OpenAI**: Upload a test prescription
   - Should process successfully
   - Check for AI-extracted medications

2. **2Factor**: Test OTP login
   - Should receive SMS
   - OTP should work

---

## ?? Troubleshooting

### **Issue: Keys not loading**

**Symptoms**:
```
?? WARNING: OpenAI API key not configured in appsettings.json
```

**Solution**:
1. Open `appsettings.json`
2. Replace `_KEY_HERE` with actual key
3. Rebuild app

---

### **Issue: Wrong environment active**

**Symptoms**:
- Using development key in production
- Features not enabled

**Solution**:
1. Check `"ActiveEnvironment"` in `appsettings.json`
2. Change to correct environment
3. Rebuild app

---

### **Issue: API calls failing**

**Check**:
1. ? Key format correct? (starts with `sk-proj-` or `sk-`)
2. ? Internet connection working?
3. ? OpenAI billing active?
4. ? API quota not exceeded?

**Test**:
```bash
# Test OpenAI API key
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"
```

---

## ?? Best Practices

### ? **DO:**

1. **Use `.gitignore`** for production configs
```gitignore
appsettings.production.json
```

2. **Keep template in Git**
```json
{
  "OpenAI": {
    "ApiKey": "YOUR_KEY_HERE"
  }
}
```

3. **Separate keys per environment**
```
Development: sk-proj-DEV...
Staging:     sk-proj-STG...
Production:  sk-proj-PROD...
```

4. **Monitor costs**
- Check OpenAI dashboard daily
- Set billing alerts
- Track usage per environment

---

### ? **DON'T:**

1. ? Commit production keys to Git
2. ? Share `appsettings.json` with real keys
3. ? Use same key for all environments
4. ? Email/Slack configuration files
5. ? Hardcode keys in source code

---

## ?? Additional Documentation

For more details, see:

- **Complete Guide**: [`01-EMBEDDED-CONFIGURATION-GUIDE.md`](01-EMBEDDED-CONFIGURATION-GUIDE.md)
- **Quick Setup**: [`02-EMBEDDED-CONFIG-QUICK-SETUP.md`](02-EMBEDDED-CONFIG-QUICK-SETUP.md)
- **Security Analysis**: [`03-SECURITY-ANALYSIS.md`](03-SECURITY-ANALYSIS.md)
- **Migration Guide**: [`04-MIGRATION-GUIDE.md`](04-MIGRATION-GUIDE.md)

---

## ? Checklist

Before deploying:

- [ ] OpenAI API key configured for target environment
- [ ] 2Factor API key configured (if using SMS OTP)
- [ ] `ActiveEnvironment` set correctly
- [ ] Tested on device
- [ ] API calls succeed
- [ ] Features enabled as desired
- [ ] Keys not committed to Git
- [ ] Billing alerts configured
- [ ] App signed for release

---

## ?? Support

### **Get Help**

1. **Check logs**: Visual Studio ? Output ? Debug
2. **Review guides**: `documentation/` folder
3. **Test keys**: Use OpenAI/2Factor dashboards

### **Common Error Messages**

```
?? WARNING: OpenAI API key not configured
? Solution: Add key to appsettings.json

? Embedded configuration file not found
? Solution: Verify appsettings.json exists and is marked as EmbeddedResource

? API call failed: 401 Unauthorized
? Solution: Check API key is valid and not expired
```

---

**Configuration Time**: 5 minutes  
**User Setup**: Zero (automatic)  
**Security**: Maximum (embedded + encrypted)  

**Your API keys are embedded, encrypted, and completely hidden! ????**
