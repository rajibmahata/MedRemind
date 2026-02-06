-- Migration: Add Authentication Tracking Fields to Users Table
-- This adds fields to track authentication method and verification status

-- Add authentication verification flags
ALTER TABLE Users
ADD IsEmailVerified INTEGER DEFAULT 0 NOT NULL;

ALTER TABLE Users
ADD IsPhoneVerified INTEGER DEFAULT 0 NOT NULL;

-- Add authentication method tracking (0=None, 1=SmsOtp, 2=EmailOtp, 3=Both)
ALTER TABLE Users
ADD AuthenticationMethod INTEGER DEFAULT 0 NOT NULL;

ALTER TABLE Users
ADD LastAuthenticationMethod INTEGER DEFAULT 0 NOT NULL;

-- Add verification timestamps
ALTER TABLE Users
ADD EmailVerifiedAt TEXT NULL;

ALTER TABLE Users
ADD PhoneVerifiedAt TEXT NULL;

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS IX_Users_IsEmailVerified ON Users(IsEmailVerified);
CREATE INDEX IF NOT EXISTS IX_Users_IsPhoneVerified ON Users(IsPhoneVerified);
CREATE INDEX IF NOT EXISTS IX_Users_AuthenticationMethod ON Users(AuthenticationMethod);

-- Verification query
-- SELECT Id, PhoneNumber, Email, IsEmailVerified, IsPhoneVerified, AuthenticationMethod, 
--        LastAuthenticationMethod, EmailVerifiedAt, PhoneVerifiedAt 
-- FROM Users;
