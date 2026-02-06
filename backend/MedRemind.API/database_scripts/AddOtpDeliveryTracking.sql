-- Migration: Add OTP Delivery Tracking Fields to Users Table
-- This adds flags to track whether email/SMS OTP was sent to users

-- Add email OTP sent flag
ALTER TABLE Users
ADD IsEmailOtpSent INTEGER NOT NULL DEFAULT 0;

-- Add SMS OTP sent flag
ALTER TABLE Users
ADD IsSmsOtpSent INTEGER NOT NULL DEFAULT 0;

-- Add last email OTP sent timestamp
ALTER TABLE Users
ADD LastEmailOtpSentAt TEXT NULL;

-- Add last SMS OTP sent timestamp
ALTER TABLE Users
ADD LastSmsOtpSentAt TEXT NULL;

-- Create indexes for OTP tracking queries
CREATE INDEX IF NOT EXISTS IX_Users_IsEmailOtpSent ON Users(IsEmailOtpSent);
CREATE INDEX IF NOT EXISTS IX_Users_IsSmsOtpSent ON Users(IsSmsOtpSent);

-- Verification query
-- SELECT Id, PhoneNumber, Email, IsEmailOtpSent, IsSmsOtpSent, LastEmailOtpSentAt, LastSmsOtpSentAt
-- FROM Users;
