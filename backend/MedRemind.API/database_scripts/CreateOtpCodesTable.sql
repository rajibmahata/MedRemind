-- Migration: Create OtpCodes Table for OTP Management
-- This table stores OTP codes with expiration and verification tracking

-- Create OtpCodes table
CREATE TABLE IF NOT EXISTS OtpCodes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PhoneNumber TEXT NOT NULL,
    Email TEXT NULL,
    UserId INTEGER NULL,
    Code TEXT NOT NULL,
    Purpose TEXT NOT NULL DEFAULT 'Login',
    DeliveryMethod TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsVerified INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    ExpiresAt TEXT NOT NULL,
    VerifiedAt TEXT NULL,
    AttemptCount INTEGER NOT NULL DEFAULT 0,
    MaxAttempts INTEGER NOT NULL DEFAULT 3,
    SenderInfo TEXT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS IX_OtpCodes_PhoneNumber ON OtpCodes(PhoneNumber);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_Email ON OtpCodes(Email);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_UserId ON OtpCodes(UserId);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_IsActive ON OtpCodes(IsActive);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_IsVerified ON OtpCodes(IsVerified);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_CreatedAt ON OtpCodes(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_ExpiresAt ON OtpCodes(ExpiresAt);
CREATE INDEX IF NOT EXISTS IX_OtpCodes_Purpose ON OtpCodes(Purpose);

-- Create composite index for common queries
CREATE INDEX IF NOT EXISTS IX_OtpCodes_PhoneNumber_IsActive_IsVerified 
ON OtpCodes(PhoneNumber, IsActive, IsVerified);

-- Verification query
-- SELECT * FROM OtpCodes ORDER BY CreatedAt DESC LIMIT 10;
