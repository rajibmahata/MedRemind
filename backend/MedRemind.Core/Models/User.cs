using System;
using MedRemind.Core.Enums;

namespace MedRemind.Core.Models;

public class User
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Mandatory email field
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public bool IsBiometricEnabled { get; set; }
    public string? SessionToken { get; set; }
    
    // Password authentication
    public string? PasswordHash { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    
    // Authentication tracking
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public AuthenticationMethod AuthenticationMethod { get; set; } = AuthenticationMethod.None;
    public AuthenticationMethod LastAuthenticationMethod { get; set; } = AuthenticationMethod.None;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    
    // OTP Delivery Tracking
    public bool IsEmailOtpSent { get; set; } = false;
    public bool IsSmsOtpSent { get; set; } = false;
    public DateTime? LastEmailOtpSentAt { get; set; }
    public DateTime? LastSmsOtpSentAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<VoiceRecording> VoiceRecordings { get; set; } = new List<VoiceRecording>();
}
