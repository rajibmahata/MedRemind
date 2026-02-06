-- Migration: Add Email Column to Users Table (MANDATORY FIELD)
-- This column stores user email addresses (required)

-- Step 1: Add Email column as nullable first (for existing records)
ALTER TABLE Users
ADD Email TEXT NULL;

-- Step 2: Update existing records with a default email if needed
-- UPDATE Users SET Email = PhoneNumber || '@placeholder.com' WHERE Email IS NULL;

-- Step 3: Make Email NOT NULL (uncomment after setting values for existing records)
-- ALTER TABLE Users ALTER COLUMN Email TEXT NOT NULL;

-- Note: SQLite doesn't support ALTER COLUMN directly
-- If you have existing records, you'll need to:
-- 1. Add Email as NULL
-- 2. Update all existing records to have valid email addresses
-- 3. Recreate table with Email as NOT NULL (see instructions below)

-- For fresh database (no existing records), use this instead:
-- DROP TABLE IF EXISTS Users;
-- CREATE TABLE Users (
--     Id INTEGER PRIMARY KEY AUTOINCREMENT,
--     PhoneNumber TEXT NOT NULL UNIQUE,
--     Email TEXT NOT NULL,
--     Name TEXT,
--     DateOfBirth TEXT,
--     Gender TEXT,
--     ProfilePhotoPath TEXT,
--     IsBiometricEnabled INTEGER DEFAULT 0,
--     SessionToken TEXT,
--     CreatedAt TEXT DEFAULT (datetime('now')),
--     LastLoginAt TEXT
-- );

-- Add unique index for email (prevents duplicate emails)
CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email 
ON Users(Email) 
WHERE Email IS NOT NULL;

-- Verification query
-- SELECT * FROM pragma_table_info('Users') WHERE name = 'Email';
