-- SQL Script: Cleanup Expired OTP Codes
-- This script removes OTP codes that have expired (older than 15 minutes)
-- Run this periodically (e.g., every 5-10 minutes) via cron job or scheduled task

-- Delete expired OTPs (older than 15 minutes)
DELETE FROM OtpCodes
WHERE datetime(CreatedAt) < datetime('now', '-15 minutes')
   OR (datetime(ExpiresAt) < datetime('now') AND IsActive = 0);

-- Show count of remaining active OTPs
SELECT COUNT(*) as ActiveOtpCount FROM OtpCodes WHERE IsActive = 1;

-- Show count of expired but not yet deleted OTPs
SELECT COUNT(*) as ExpiredOtpCount 
FROM OtpCodes 
WHERE datetime(ExpiresAt) < datetime('now') AND IsActive = 1;

-- Optional: Show recent cleanup statistics
SELECT 
    'Cleanup Statistics' as Info,
    (SELECT COUNT(*) FROM OtpCodes WHERE IsActive = 1) as ActiveOtps,
    (SELECT COUNT(*) FROM OtpCodes WHERE IsActive = 0) as InactiveOtps,
    (SELECT COUNT(*) FROM OtpCodes WHERE IsVerified = 1) as VerifiedOtps,
    (SELECT COUNT(*) FROM OtpCodes WHERE datetime(ExpiresAt) < datetime('now')) as ExpiredOtps;
