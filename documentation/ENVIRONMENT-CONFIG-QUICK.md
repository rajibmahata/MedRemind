# ? Environment-Based Configuration - Quick Reference

## ?? Three Environments

| Environment | Icon | Use Case | Cost |
|-------------|------|----------|------|
| **Development** | ?? | Testing | Minimal |
| **Staging** | ?? | Pre-production | Moderate |
| **Production** | ?? | Live users | Variable |

---

## ?? Key Features

? Separate API keys per environment  
? AES-256 encryption  
? Survives app updates  
? Zero-impact updates  
? Version tracking  

---

## ?? How to Use

### **Switch Environment**
```
Settings ? API Configuration ? Change ? Select environment
```

### **Configure Keys**
```
1. Select environment
2. Tap "Configure"
3. Enter API key
4. Test connection
5. Done!
```

### **View All Environments**
```
API Configuration ? View All Environments
Shows which environments are configured
```

---

## ?? Environment Switching

```
Development ??  ? Configure test key
     ?
Staging ??      ? Configure staging key
     ?
Production ??   ? Configure production key
```

**Each environment has separate, encrypted keys!**

---

## ?? Persistence

| Action | Keys Preserved? |
|--------|-----------------|
| App Update | ? Yes |
| App Restart | ? Yes |
| Device Reboot | ? Yes |
| App Uninstall | ? No |
| Clear App Data | ? No |

---

## ?? Security

```
Plain Text Key ? AES-256 Encryption ? Encrypted Storage
                    ?
            Device-Specific Key
                    ?
        Cannot decrypt on other devices
```

---

## ?? Quick Actions

| Task | Steps |
|------|-------|
| **Switch to Dev** | Change ? Development |
| **Switch to Prod** | Change ? Production |
| **Test Connection** | Configure ? Test |
| **View Status** | View All Environments |
| **Update Key** | Configure ? Enter new key |

---

## ?? Cost per Environment

**Development**: < $5/month (testing)  
**Staging**: $10-20/month (pre-prod)  
**Production**: Variable (live users)  

**Tip**: Use separate OpenAI accounts for each environment

---

## ? Checklist

### **Development Setup**
- [ ] Switch to Development
- [ ] Configure test API key
- [ ] Test connection
- [ ] Test prescription scanning

### **Production Setup**
- [ ] Switch to Production
- [ ] Configure production API key
- [ ] Test connection
- [ ] Monitor costs

---

## ?? Quick Fixes

**Keys not loading?**  
? Restart app

**Wrong environment?**  
? Check "Current:" at top of page

**Test fails?**  
? Verify key for current environment

**After update, keys gone?**  
? They're not! Restart app to reload

---

## ?? Status Indicators

? Configured - Ready to use  
?? Not Configured - Need to configure  
?? Encrypted - Keys are secure  
?? Visible - Keys are shown  

---

**Time to Setup**: 2 minutes per environment  
**Security**: AES-256 encrypted  
**Persistence**: Survives updates  

---

**Your keys are now environment-aware! ??**
