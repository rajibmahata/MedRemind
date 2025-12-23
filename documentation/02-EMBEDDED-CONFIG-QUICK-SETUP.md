# ? Embedded Configuration - Quick Setup

## ?? Overview

API keys are now **embedded in the app binary** and **encrypted at runtime**. No user configuration needed!

---

## ?? Configuration File

**Location**: `mobile/MedRemind.Mobile/appsettings.json`

```json
{
  "Environments": {
    "Development": {
      "OpenAI": {
        "ApiKey": "sk-proj-DEV_KEY"
      }
    },
    "Production": {
      "OpenAI": {
        "ApiKey": "sk-proj-PROD_KEY"
      }
    }
  },
  "ActiveEnvironment": "Production"
}
```

---

## ?? Quick Setup (3 Steps)

### **Step 1: Add Your Keys**

Edit `appsettings.json`:

```json
{
  "Environments": {
    "Production": {
      "OpenAI": {
        "ApiKey": "sk-proj-YOUR-ACTUAL-KEY-HERE"
      }
    }
  },
  "ActiveEnvironment": "Production"
}
```

### **Step 2: Build**

```bash
dotnet build -c Release
```

### **Step 3: Deploy**

```
Deploy APK ? Keys embedded ? Works immediately!
```

---

## ?? Security

| Feature | Status |
|---------|--------|
| Embedded in binary | ? |
| Runtime encryption | ? |
| Device-specific | ? |
| Cannot extract | ? |
| Hidden from users | ? |

---

## ?? Different Environments

### **Development Build**

```json
"ActiveEnvironment": "Development"
```

```bash
dotnet build -c Debug
```

### **Production Build**

```json
"ActiveEnvironment": "Production"
```

```bash
dotnet build -c Release
```

---

## ?? Update Keys

```
1. Edit appsettings.json
2. Update API keys
3. Rebuild app
4. Redeploy
```

**Users get new keys automatically on app update!**

---

## ? Verification

### **Check Embedded Resource**

Run app and check logs:

```
? Embedded configuration loaded - Active Environment: Production
? OpenAI API key loaded from embedded config
```

### **Test API Call**

```
1. Open app
2. Upload ? Select prescription
3. Process with AI
4. Should work without any configuration!
```

---

## ?? Benefits

| Benefit | Description |
|---------|-------------|
| **Zero Config** | Users don't configure anything |
| **Secure** | Keys encrypted, cannot extract |
| **Simple** | One config file |
| **Flexible** | Multiple environments |
| **Updates** | Change keys by rebuilding |

---

## ?? Troubleshooting

**Keys not loading?**
? Check appsettings.json has actual keys (not placeholders)

**Wrong environment?**
? Check `ActiveEnvironment` in appsettings.json

**Build fails?**
? Ensure appsettings.json is marked as EmbeddedResource in .csproj

---

## ?? .gitignore

Add to `.gitignore` to protect production keys:

```gitignore
# Production configuration
appsettings.production.json
**/appsettings.*.json
```

Keep template in Git:

```json
{
  "ActiveEnvironment": "Development",
  "Environments": {
    "Development": {
      "OpenAI": {
        "ApiKey": "YOUR_KEY_HERE"
      }
    }
  }
}
```

---

**Time**: 3 minutes  
**Difficulty**: Easy  
**Security**: Maximum  
**User Config**: Zero  

**Your keys are now embedded and secure! ??**
