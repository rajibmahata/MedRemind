# 2Factor.in SMS Integration Guide

## Overview

MedRemind uses **2Factor.in** as the SMS service provider for sending OTP (One-Time Password) and medication reminder messages to users. 2Factor.in is a cost-effective SMS gateway service popular in India with reliable delivery rates.

**Documentation Version:** 1.0  
**Last Updated:** December 20, 2025  
**Provider:** 2Factor.in (https://2factor.in)

---

## Table of Contents

1. [Why 2Factor.in](#why-2factorin)
2. [Account Setup](#account-setup)
3. [API Overview](#api-overview)
4. [Authentication Flow Implementation](#authentication-flow-implementation)
5. [SMS Templates](#sms-templates)
6. [Code Implementation](#code-implementation)
7. [Error Handling](#error-handling)
8. [Cost Analysis](#cost-analysis)
9. [Testing](#testing)
10. [Best Practices](#best-practices)
11. [Troubleshooting](#troubleshooting)

---

## Why 2Factor.in

### Advantages

1. **Cost-Effective**: ₹0.10-0.15 per SMS in India
2. **High Delivery Rate**: 95%+ delivery success
3. **Fast Delivery**: Average delivery time < 5 seconds
4. **OTP Specialization**: Built specifically for OTP delivery
5. **No Setup Fee**: Pay-as-you-go model
6. **DND Compliance**: Works on DND numbers for transactional SMS
7. **Global Coverage**: Supports international SMS
8. **Simple API**: RESTful API, easy to integrate
9. **Dashboard**: Real-time analytics and monitoring
10. **Support**: 24/7 customer support

### Comparison with Alternatives

| Feature | 2Factor.in | Twilio | AWS SNS |
|---------|------------|--------|---------|
| Cost (India SMS) | ₹0.10-0.15 | $0.0075 (~₹0.62) | $0.00645 (~₹0.53) |
| Setup Complexity | Easy | Medium | Complex |
| India Focus | Yes | No | No |
| OTP Specialization | Yes | No | No |
| Minimum Balance | ₹500 | $20 | None |
| DND Bypass | Yes | Yes | Yes |

**Winner for India Market**: 2Factor.in offers the best combination of cost, ease of use, and India-specific features.

---

## Account Setup

### Step 1: Create Account

1. Visit https://2factor.in
2. Click "Sign Up" button
3. Enter business details:
   - Company name: MedRemind
   - Email address
   - Phone number
   - Password
4. Verify email address
5. Complete KYC (Know Your Customer):
   - Business registration documents
   - ID proof
   - Address proof

### Step 2: Get API Key

1. Login to 2Factor.in dashboard
2. Navigate to **Developer API** section
3. Copy your **API Key**
4. Store securely in environment variables

### Step 3: Configure Sender ID

1. Navigate to **Sender ID** section
2. Request a 6-character Sender ID (e.g., "MEDRMD")
3. Wait for approval (1-2 business days)
4. Use approved Sender ID in API calls

---

## API Overview

### Base URL
https://2factor.in/API/V1/

### Available Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| /SendSMS | GET | Send custom SMS |
| /SendOTP | GET | Send OTP SMS |
| /VerifyOTP | GET | Verify OTP |
| /CheckBalance | GET | Check account balance |

---

## Code Implementation (.NET C#)

```csharp
// Services/TwoFactorSMSService.cs
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MedRemind.Services
{
    public interface ITwoFactorSMSService
    {
        Task<OTPResponse> SendOTPAsync(string phoneNumber);
        Task<bool> VerifyOTPAsync(string sessionId, string otp);
        Task<SMSResponse> SendCustomSMSAsync(string phoneNumber, string message);
    }
    
    public class TwoFactorSMSService : ITwoFactorSMSService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://2factor.in/API/V1";
        
        public TwoFactorSMSService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["TwoFactor:ApiKey"];
        }
        
        public async Task<OTPResponse> SendOTPAsync(string phoneNumber)
        {
            try
            {
                var otp = GenerateOTP();
                phoneNumber = CleanPhoneNumber(phoneNumber);
                
                var url = $"{_baseUrl}/{_apiKey}/SMS/{phoneNumber}/{otp}/MEDRMD";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TwoFactorResponse>(content);
                    
                    return new OTPResponse
                    {
                        IsSuccess = result.Status == "Success",
                        SessionId = result.Details,
                        OTP = otp,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                        Message = "OTP sent successfully"
                    };
                }
                
                return new OTPResponse
                {
                    IsSuccess = false,
                    Message = $"Failed to send OTP: {content}"
                };
            }
            catch (Exception ex)
            {
                return new OTPResponse
                {
                    IsSuccess = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        
        public async Task<bool> VerifyOTPAsync(string sessionId, string otp)
        {
            try
            {
                var url = $"{_baseUrl}/{_apiKey}/SMS/VERIFY/{sessionId}/{otp}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TwoFactorResponse>(content);
                    return result.Status == "Success";
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        
        private string CleanPhoneNumber(string phoneNumber)
        {
            phoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            if (phoneNumber.StartsWith("91") && phoneNumber.Length == 12)
            {
                phoneNumber = phoneNumber.Substring(2);
            }
            
            if (phoneNumber.Length != 10)
            {
                throw new ArgumentException("Invalid phone number format");
            }
            
            return phoneNumber;
        }
    }
    
    public class OTPResponse
    {
        public bool IsSuccess { get; set; }
        public string SessionId { get; set; }
        public string OTP { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; }
    }
    
    public class SMSResponse
    {
        public bool IsSuccess { get; set; }
        public string MessageId { get; set; }
        public string Message { get; set; }
    }
    
    internal class TwoFactorResponse
    {
        public string Status { get; set; }
        public string Details { get; set; }
    }
}
```

---

## Cost Analysis

### Pricing Structure

| Service | Cost per Unit |
|---------|---------------|
| Transactional SMS (India) | ₹0.10-0.15 |
| International SMS | ₹3-5 |
| OTP SMS | ₹0.10-0.15 |

### Monthly Cost Estimates

**100 Active Users**
- Total SMS: 6,400/month
- Cost: ₹960/month (~$12)

**1,000 Active Users**
- Total SMS: 64,000/month
- Cost: ₹8,320/month (~$100)

**10,000 Active Users**
- Total SMS: 640,000/month
- Cost: ₹64,000/month (~$770)

---

## Best Practices

1. **Security**: Never log OTPs in plain text
2. **Rate Limiting**: Max 3 OTP requests per hour per number
3. **Short Expiry**: Keep OTP valid for only 5 minutes
4. **One-time Use**: Delete OTP after successful verification
5. **Retry Logic**: Implement exponential backoff for failures

---

## Configuration

Add to appsettings.json:

```json
{
  "TwoFactor": {
    "ApiKey": "your_api_key_here",
    "SenderId": "MEDRMD",
    "OtpExpiry": 300
  }
}
```

---

[← Back to Database Schema](03-database-schema.md) | [Next: Authentication Epic →](05-epic-01-authentication.md)