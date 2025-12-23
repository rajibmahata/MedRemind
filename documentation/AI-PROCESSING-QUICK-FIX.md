# ? AI Processing Not Working - 5 Minute Fix

## ? Problem
The OpenAI API key is not configured (set to placeholder `"your-openai-api-key-here"`).

---

## ? Quick Fix

### **Step 1: Get OpenAI API Key (2 min)**

1. Go to https://platform.openai.com/api-keys
2. Click "Create new secret key"
3. Copy the key (starts with `sk-proj-...`)
4. **SAVE IT** - you can't see it again!

### **Step 2: Update MauiProgram.cs (1 min)**

**File**: `mobile/MedRemind.Mobile/MauiProgram.cs`

**Find line 58**:
```csharp
var apiKey = "your-openai-api-key-here";
```

**Replace with**:
```csharp
var apiKey = "sk-proj-YOUR_ACTUAL_KEY_HERE";
```

### **Step 3: Rebuild (2 min)**

```
1. Build ? Clean Solution
2. Build ? Rebuild Solution
3. Press F5 to deploy
```

### **Step 4: Test**

```
1. Open app ? Upload tab
2. Select/capture prescription image
3. Tap "Process with AI"
4. Wait 5-10 seconds
5. See extracted medications ?
```

---

## ?? Cost

- **Per scan**: $0.01 - $0.05
- **Minimum deposit**: $5 (100-500 scans)
- **Add payment**: https://platform.openai.com/account/billing

---

## ?? If Still Not Working

### Check 1: API Key Valid?
```
Starts with: sk-proj- ?
Has spaces: ? Remove them
Has quotes: ? Remove them
```

### Check 2: OpenAI Credits?
```
Go to: https://platform.openai.com/account/billing
Balance: Must be > $0
```

### Check 3: Internet Working?
```
Can browse web? ?
Phone has data? ?
```

### Check 4: See Logs
```
Visual Studio ? View ? Output ? Debug
Look for errors like:
- "401 Unauthorized" ? Bad API key
- "429 Too Many Requests" ? No credits
- "500 Internal Server Error" ? OpenAI issue
```

---

## ?? What You'll See

### Before Fix:
```
[Image selected]
?
Tap "Process with AI"
?
Spinning... then ERROR
```

### After Fix:
```
[Image selected]
?
Tap "Process with AI"
?
Spinning 5-10 seconds
?
? Confidence: 92%
? Medications extracted
? Ready to save
```

---

## ?? Success Checklist

- [ ] Got OpenAI API key
- [ ] Added payment method ($5+)
- [ ] Updated MauiProgram.cs
- [ ] Rebuilt app
- [ ] Redeployed to phone
- [ ] Processing works!

---

**Time**: 5 minutes  
**Cost**: $5 minimum  
**Result**: Full AI prescription scanning  

---

**Get your key now**: https://platform.openai.com/api-keys ??
