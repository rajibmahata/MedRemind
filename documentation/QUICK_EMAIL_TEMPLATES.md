# Quick Reference - Email Templates

## Summary
Modern HTML email templates for OTP and Welcome emails with automatic sending after registration.

## Templates Created

```
backend\MedRemind.Core\EmailTemplates\
??? OtpEmail.html       ? Modern OTP code email
??? WelcomeEmail.html   ? Welcome new users
```

## Features

### OTP Email
- ?? Purple gradient header
- ?? Large OTP code display
- ? 10-minute expiration notice
- ?? Security tips
- ?? Mobile responsive

### Welcome Email
- ?? Welcoming design
- ?? Account details card
- ?? Feature highlights
- ?? Getting started tips
- ?? CTA button

## Registration Flow

```
User Registers
   ?
Create User (unverified)
   ?
Send OTP (Modern Template)
   ?
Send Welcome Email
   ?
User receives both emails
```

## Quick Test

```bash
# Register user
POST /api/users/register
{
  "phoneNumber": "9876543210",
  "email": "test@example.com",
  "name": "John Doe"
}

# Check email inbox:
# 1. OTP email (modern design)
# 2. Welcome email (feature highlights)
```

## Template Placeholders

### OTP Email
- `{{UserName}}` - User's name
- `{{OtpCode}}` - 6-digit code

### Welcome Email
- `{{UserName}}` - User's name
- `{{UserEmail}}` - Email address
- `{{UserPhone}}` - Phone number
- `{{RegistrationDate}}` - Date registered
- `{{AppLink}}` - App URL

## Customization

### Change Colors
Edit templates in:
```
backend\MedRemind.Core\EmailTemplates\
```

### OTP Email Header
```css
.header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
```

### Welcome Email Button
```css
.cta-button {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
```

## Add New Template

1. Create HTML file:
   ```
   backend\MedRemind.Core\EmailTemplates\YourTemplate.html
   ```

2. Add method to `EmailTemplateService`:
   ```csharp
   public string GetYourTemplate(string param)
   {
       var template = LoadTemplate("YourTemplate.html");
       return ReplacePlaceholders(template, placeholders);
   }
   ```

3. Add method to `EmailService`:
   ```csharp
   public async Task<(bool, string?)> SendYourEmailAsync(...)
   {
       var html = _templateService.GetYourTemplate(...);
       // Send email...
   }
   ```

## Email Preview

### OTP Email
```
??????????????????????
?  ?? MedRemind      ? ? Purple gradient
??????????????????????
? Hello John! ??     ?
?                    ?
? Your OTP:          ?
? ????????????       ?
? ? 123456   ?       ? ? Large code
? ????????????       ?
?                    ?
? ? Valid 10 min    ?
? ?? Security tips   ?
??????????????????????
```

### Welcome Email
```
??????????????????????
?       ??           ?
? Welcome to         ?
?   MedRemind!       ? ? Large header
??????????????????????
? Hello John! ??     ?
?                    ?
? ?? Account Details ?
? Name: John Doe     ?
? Email: john@...    ?
?                    ?
? ?? Features:       ?
? ? Smart Reminders ?
? ?? Scanner         ?
? ?? Tracking        ?
?                    ?
? ??????????????     ?
? ? Open App   ?     ? ? Button
? ??????????????     ?
??????????????????????
```

## Benefits

? **Professional** - Modern gradient design  
? **Personal** - User name in greetings  
? **Clear** - Easy-to-read OTP codes  
? **Helpful** - Getting started tips  
? **Responsive** - Works on mobile  
? **Maintainable** - Easy to update  

## Build Status

? **Build Successful** - Ready to use

---

**Full Documentation:** See `EMAIL_TEMPLATES_IMPLEMENTATION.md`
