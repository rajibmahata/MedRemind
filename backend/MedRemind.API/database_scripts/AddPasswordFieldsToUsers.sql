-- Migration: Add Password Fields to Users Table
-- This adds password authentication fields to support password-based login

-- Add password hash field
ALTER TABLE Users
ADD PasswordHash TEXT NULL;

-- Add password reset token
ALTER TABLE Users
ADD PasswordResetToken TEXT NULL;

-- Add password reset token expiry
ALTER TABLE Users
ADD PasswordResetTokenExpiry TEXT NULL;

-- Add last password change timestamp
ALTER TABLE Users
ADD LastPasswordChangeAt TEXT NULL;

-- Create index for password reset token lookups
CREATE INDEX IF NOT EXISTS IX_Users_PasswordResetToken ON Users(PasswordResetToken);

-- Verification query
-- SELECT Id, PhoneNumber, Email, PasswordHash, PasswordResetToken, PasswordResetTokenExpiry, LastPasswordChangeAt
-- FROM Users;
