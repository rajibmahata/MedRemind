# ?? Sensitive Data Protection Guide

## Overview

This guide explains how to protect sensitive information in your MedRemind project, including API keys, phone numbers, and other confidential data.

---

## ?? Sensitive Data Types

### **1. API Keys**
- OpenAI API keys
- 2Factor API keys
- Any third-party service credentials

### **2. Personal Information**
- Phone numbers
- User IDs
- Email addresses
- Session tokens

### **3. Configuration Files**
- `appsettings.json` with real credentials
- Environment-specific configs
- Database connection strings

---

## ??? Protection Methods

### **1. Use Placeholder Values**

**Bad** ?:
```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-abc123xyz789..."
  }
}
```

**Good** ?:
```json
{
  "OpenAI": {
    "ApiKey": "YOUR-OPENAI-API-KEY-HERE"
  }
}
```

### **2. Git Ignore Sensitive Files**

Add to `.gitignore`:
```gitignore
# Sensitive configuration files
**/appsettings.json
**/appsettings.*.json
**/*.config.user

# Environment files
.env
.env.local
.env.production

# User-specific files
*.user
*.suo
*.userosscache
```

### **3. Mask Data in Logs**

**Bad** ?:
```csharp
Debug.WriteLine($"Sending OTP to {phoneNumber}");
Debug.WriteLine($"API Key: {apiKey}");
```

**Good** ?:
```csharp
Debug.WriteLine($"Sending OTP to ***{phoneNumber.Substring(7)}");
Debug.WriteLine($"API Key: {apiKey.Substring(0, 8)}...[MASKED]");
```

### **4. Use Environment Variables**

**Production**:
```csharp
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var twoFactorKey = Environment.GetEnvironmentVariable("TWOFACTOR_API_KEY");
```

---

## ?? Documentation Guidelines

### **Mask Examples**

| Data Type | Example | Masked Version |
|-----------|---------|----------------|
| **Phone Number** | 9100184730 | XXXXXXXXXX |
| **API Key** | 3d128e27-e07f-11f0-a6b2-0200cd936042 | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx |
| **OTP Code** | 123456 | XXXXXX |
| **Session ID** | 6c4e560a-f550-40e5-9eec-02d887ca2f17 | [MASKED] |
| **User ID** | 123 | [USER_ID] |

### **Code Examples**

Use placeholders:
```csharp
// Example: Send OTP
var phoneNumber = "XXXXXXXXXX";  // 10-digit phone
var apiKey = "YOUR-API-KEY-HERE";
var otp = "XXXXXX";  // 6-digit OTP
```

---

## ?? Configuration Management

### **Development Setup**

1. **Copy template**:
```bash
cp appsettings.template.json appsettings.json
```

2. **Add real keys** to `appsettings.json` (local only)

3. **Keep template** in Git with placeholders

### **Template File** (`appsettings.template.json`)

```json
{
  "Environments": {
    "Development": {
      "OpenAI": {
        "ApiKey": "YOUR-OPENAI-DEV-API-KEY-HERE"
      },
      "TwoFactor": {
        "ApiKey": "YOUR-2FACTOR-DEV-API-KEY-HERE"
      }
    }
  }
}
```

---

## ?? Deployment Best Practices

### **1. Environment Variables**

Set in deployment platform:
```bash
OPENAI_API_KEY=sk-proj-real-key
TWOFACTOR_API_KEY=real-key-here
```

### **2. Azure Key Vault** (Recommended for Azure)

```csharp
var keyVaultClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
var openAiKey = await keyVaultClient.GetSecretAsync("OpenAI-ApiKey");
```

### **3. AWS Secrets Manager** (for AWS)

```csharp
var client = new AmazonSecretsManagerClient();
var secret = await client.GetSecretValueAsync(new GetSecretValueRequest
{
    SecretId = "medremind/openai-key"
});
```

---

## ? Security Checklist

### **Before Committing**

- [ ] Check for API keys in files
- [ ] Verify phone numbers are masked
- [ ] Ensure OTPs are not logged
- [ ] Review `.gitignore` entries
- [ ] Scan for sensitive data in diffs

### **Code Review**

- [ ] No hardcoded credentials
- [ ] Proper data masking in logs
- [ ] Environment variables used
- [ ] Configuration templates included
- [ ] Documentation uses placeholders

### **Deployment**

- [ ] Production keys in secure storage
- [ ] Environment variables configured
- [ ] Access controls in place
- [ ] Audit logging enabled
- [ ] Backup keys secured

---

## ?? Finding Sensitive Data

### **Search Patterns**

```bash
# Find API keys
grep -r "sk-proj-" .
grep -r "api.*key.*:" .

# Find phone numbers
grep -r "\+91[0-9]\{10\}" .
grep -r "[0-9]\{10\}" .

# Find GUIDs (potential session IDs)
grep -r "[a-f0-9]\{8\}-[a-f0-9]\{4\}-[a-f0-9]\{4\}-[a-f0-9]\{4\}-[a-f0-9]\{12\}" .
```

### **Git History Scan**

```bash
# Check what would be committed
git status

# Check diff before commit
git diff

# Search git history for secrets
git log -p | grep -i "api.*key"
```

---

## ?? If Credentials Are Compromised

### **Immediate Actions**

1. **Revoke compromised keys immediately**
2. **Generate new keys**
3. **Update all deployments**
4. **Review access logs**
5. **Notify security team**

### **OpenAI Key Compromised**

1. Go to https://platform.openai.com/api-keys
2. Delete compromised key
3. Generate new key
4. Update all environments

### **2Factor Key Compromised**

1. Go to https://2factor.in/
2. Regenerate API key
3. Update configuration
4. Monitor usage reports

---

## ?? Additional Resources

### **Tools**

- **git-secrets**: Prevents committing secrets
- **trufflehog**: Scans git history for secrets
- **gitleaks**: Detects hardcoded secrets

### **Installation**

```bash
# git-secrets
brew install git-secrets
git secrets --install
git secrets --register-aws

# gitleaks
brew install gitleaks
gitleaks detect --source .
```

---

## ?? Summary

### **Key Principles**

1. ? **Never commit real credentials**
2. ? **Use placeholders in documentation**
3. ? **Mask sensitive data in logs**
4. ? **Keep configuration templates in Git**
5. ? **Use environment variables in production**
6. ? **Regular security audits**

### **Quick Commands**

```bash
# Check before commit
git diff --cached

# Search for potential secrets
grep -r "sk-proj-" .

# Remove file from Git history
git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch appsettings.json' \
  --prune-empty --tag-name-filter cat -- --all
```

---

**Remember**: Security is not optional! Protect your credentials and user data. ??
