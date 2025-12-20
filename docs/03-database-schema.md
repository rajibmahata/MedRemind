# Database Schema Documentation

## Overview

MedRemind uses SQLite as its local database system to store all medication-related data, user preferences, and reminder information. This document provides a complete reference for the database schema, including all tables, relationships, indexes, and SQL scripts.

**Database Version:** 1.0  
**Last Updated:** 2025-12-20  
**Database Type:** SQLite 3.x

---

## Table of Contents

1. [Database Design Principles](#database-design-principles)
2. [Entity Relationship Diagram](#entity-relationship-diagram)
3. [Tables](#tables)
4. [Relationships](#relationships)
5. [Indexes](#indexes)
6. [SQL Scripts](#sql-scripts)
7. [Data Types and Constraints](#data-types-and-constraints)
8. [Sample Data](#sample-data)
9. [Migration Scripts](#migration-scripts)

---

## Database Design Principles

- **Normalization:** Database follows 3NF (Third Normal Form) to minimize redundancy
- **Data Integrity:** Foreign key constraints ensure referential integrity
- **Performance:** Strategic indexes on frequently queried columns
- **Scalability:** Designed to handle thousands of medications and reminders
- **Privacy:** All data stored locally on device with optional encryption
- **Backup:** Schema supports easy export/import for data backup

---

## Entity Relationship Diagram

```
┌─────────────────┐
│     Users       │
│─────────────────│
│ user_id (PK)    │
│ username        │
│ email           │
│ password_hash   │
│ created_at      │
│ updated_at      │
└────────┬────────┘
         │
         │ 1:N
         │
┌────────▼────────────┐
│    Medications      │
│─────────────────────│
│ medication_id (PK)  │
│ user_id (FK)        │
│ name                │
│ dosage              │
│ frequency           │
│ start_date          │
│ end_date            │
│ instructions        │
│ created_at          │
│ updated_at          │
└────────┬────────────┘
         │
         │ 1:N
         │
┌────────▼────────────┐
│     Reminders       │
│─────────────────────│
│ reminder_id (PK)    │
│ medication_id (FK)  │
│ reminder_time       │
│ is_active           │
│ repeat_days         │
│ created_at          │
│ updated_at          │
└────────┬────────────┘
         │
         │ 1:N
         │
┌────────▼────────────┐
│   Reminder_Logs     │
│─────────────────────│
│ log_id (PK)         │
│ reminder_id (FK)    │
│ status              │
│ taken_at            │
│ skipped_at          │
│ notes               │
│ created_at          │
└─────────────────────┘

┌─────────────────────┐
│    Prescriptions    │
│─────────────────────│
│ prescription_id(PK) │
│ medication_id (FK)  │
│ doctor_name         │
│ prescription_date   │
│ image_path          │
│ created_at          │
└─────────────────────┘

┌─────────────────────┐
│   User_Settings     │
│─────────────────────│
│ setting_id (PK)     │
│ user_id (FK)        │
│ notification_sound  │
│ snooze_duration     │
│ dark_mode           │
│ language            │
│ created_at          │
│ updated_at          │
└─────────────────────┘
```

---

## Tables

### 1. Users Table

Stores user account information and authentication details.

```sql
CREATE TABLE users (
    user_id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    date_of_birth DATE,
    phone_number TEXT,
    profile_image_path TEXT,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**Columns:**
- `user_id`: Unique identifier for each user
- `username`: Unique username for login
- `email`: User's email address (unique)
- `password_hash`: Hashed password (bcrypt/Argon2)
- `first_name`: User's first name
- `last_name`: User's last name
- `date_of_birth`: User's date of birth
- `phone_number`: Contact phone number
- `profile_image_path`: Path to profile image
- `is_active`: Account status flag
- `created_at`: Account creation timestamp
- `updated_at`: Last update timestamp

---

### 2. Medications Table

Stores information about medications added by users.

```sql
CREATE TABLE medications (
    medication_id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    generic_name TEXT,
    dosage TEXT NOT NULL,
    dosage_unit TEXT DEFAULT 'mg',
    frequency TEXT NOT NULL,
    frequency_type TEXT DEFAULT 'daily',
    start_date DATE NOT NULL,
    end_date DATE,
    total_quantity INTEGER,
    remaining_quantity INTEGER,
    instructions TEXT,
    side_effects TEXT,
    category TEXT,
    color_code TEXT DEFAULT '#3498db',
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
```

**Columns:**
- `medication_id`: Unique identifier for each medication
- `user_id`: Reference to the user who added the medication
- `name`: Brand/commercial name of the medication
- `generic_name`: Generic/scientific name
- `dosage`: Dosage amount (e.g., "500")
- `dosage_unit`: Unit of measurement (mg, ml, tablets, etc.)
- `frequency`: How often to take (e.g., "twice daily", "every 8 hours")
- `frequency_type`: Type of frequency (daily, weekly, as_needed)
- `start_date`: When to start taking the medication
- `end_date`: When to stop (NULL for ongoing)
- `total_quantity`: Total pills/doses in supply
- `remaining_quantity`: Remaining doses
- `instructions`: Special instructions
- `side_effects`: Known side effects
- `category`: Medication category (antibiotic, painkiller, etc.)
- `color_code`: UI color for visual identification
- `is_active`: Whether medication is currently active
- `created_at`: Record creation timestamp
- `updated_at`: Last update timestamp

---

### 3. Reminders Table

Stores reminder schedules for medications.

```sql
CREATE TABLE reminders (
    reminder_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id INTEGER NOT NULL,
    reminder_time TIME NOT NULL,
    repeat_days TEXT DEFAULT '1,2,3,4,5,6,7',
    is_active BOOLEAN DEFAULT 1,
    snooze_count INTEGER DEFAULT 0,
    max_snooze INTEGER DEFAULT 3,
    advance_notification INTEGER DEFAULT 0,
    notification_enabled BOOLEAN DEFAULT 1,
    vibration_enabled BOOLEAN DEFAULT 1,
    sound_enabled BOOLEAN DEFAULT 1,
    custom_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE
);
```

**Columns:**
- `reminder_id`: Unique identifier for each reminder
- `medication_id`: Reference to the medication
- `reminder_time`: Time of day for reminder (HH:MM:SS)
- `repeat_days`: Comma-separated days (1=Monday, 7=Sunday)
- `is_active`: Whether reminder is active
- `snooze_count`: Current snooze count
- `max_snooze`: Maximum allowed snoozes
- `advance_notification`: Minutes before to show notification
- `notification_enabled`: Push notification flag
- `vibration_enabled`: Vibration flag
- `sound_enabled`: Sound flag
- `custom_message`: Custom reminder message
- `created_at`: Record creation timestamp
- `updated_at`: Last update timestamp

---

### 4. Reminder_Logs Table

Tracks the history of reminder interactions.

```sql
CREATE TABLE reminder_logs (
    log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    reminder_id INTEGER NOT NULL,
    medication_id INTEGER NOT NULL,
    scheduled_time TIMESTAMP NOT NULL,
    status TEXT NOT NULL CHECK(status IN ('taken', 'skipped', 'missed', 'snoozed')),
    taken_at TIMESTAMP,
    skipped_at TIMESTAMP,
    missed_at TIMESTAMP,
    snoozed_at TIMESTAMP,
    snooze_until TIMESTAMP,
    notes TEXT,
    dosage_taken TEXT,
    location TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (reminder_id) REFERENCES reminders(reminder_id) ON DELETE CASCADE,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE
);
```

**Columns:**
- `log_id`: Unique identifier for each log entry
- `reminder_id`: Reference to the reminder
- `medication_id`: Reference to the medication
- `scheduled_time`: When reminder was scheduled
- `status`: Status of reminder (taken, skipped, missed, snoozed)
- `taken_at`: Timestamp when marked as taken
- `skipped_at`: Timestamp when skipped
- `missed_at`: Timestamp when marked as missed
- `snoozed_at`: Timestamp when snoozed
- `snooze_until`: When to remind again after snooze
- `notes`: User notes about this instance
- `dosage_taken`: Actual dosage taken (if different)
- `location`: GPS location where taken (optional)
- `created_at`: Log creation timestamp

---

### 5. Prescriptions Table

Stores prescription information and images.

```sql
CREATE TABLE prescriptions (
    prescription_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    doctor_name TEXT,
    doctor_contact TEXT,
    hospital_name TEXT,
    prescription_date DATE NOT NULL,
    expiry_date DATE,
    prescription_number TEXT,
    image_path TEXT,
    pdf_path TEXT,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
```

**Columns:**
- `prescription_id`: Unique identifier
- `medication_id`: Reference to medication
- `user_id`: Reference to user
- `doctor_name`: Prescribing doctor's name
- `doctor_contact`: Doctor's contact information
- `hospital_name`: Hospital/clinic name
- `prescription_date`: Date of prescription
- `expiry_date`: Prescription expiry date
- `prescription_number`: Prescription reference number
- `image_path`: Path to prescription image
- `pdf_path`: Path to prescription PDF
- `notes`: Additional notes
- `created_at`: Record creation timestamp
- `updated_at`: Last update timestamp

---

### 6. User_Settings Table

Stores user preferences and application settings.

```sql
CREATE TABLE user_settings (
    setting_id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL UNIQUE,
    notification_sound TEXT DEFAULT 'default',
    notification_volume INTEGER DEFAULT 80,
    snooze_duration INTEGER DEFAULT 5,
    dark_mode BOOLEAN DEFAULT 0,
    language TEXT DEFAULT 'en',
    time_format TEXT DEFAULT '24h',
    date_format TEXT DEFAULT 'YYYY-MM-DD',
    auto_backup BOOLEAN DEFAULT 1,
    backup_frequency TEXT DEFAULT 'weekly',
    reminder_before_stock_ends INTEGER DEFAULT 7,
    pin_code TEXT,
    biometric_enabled BOOLEAN DEFAULT 0,
    sync_enabled BOOLEAN DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
```

**Columns:**
- `setting_id`: Unique identifier
- `user_id`: Reference to user (unique)
- `notification_sound`: Notification sound file name
- `notification_volume`: Volume level (0-100)
- `snooze_duration`: Default snooze duration in minutes
- `dark_mode`: Dark mode preference
- `language`: Preferred language code
- `time_format`: 12h or 24h format
- `date_format`: Date display format
- `auto_backup`: Auto backup enabled
- `backup_frequency`: How often to backup
- `reminder_before_stock_ends`: Days before stock ends to alert
- `pin_code`: Hashed PIN for app lock
- `biometric_enabled`: Fingerprint/Face ID enabled
- `sync_enabled`: Cloud sync enabled
- `created_at`: Record creation timestamp
- `updated_at`: Last update timestamp

---

### 7. Medication_Interactions Table

Stores potential drug interaction warnings.

```sql
CREATE TABLE medication_interactions (
    interaction_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id_1 INTEGER NOT NULL,
    medication_id_2 INTEGER NOT NULL,
    severity TEXT CHECK(severity IN ('minor', 'moderate', 'severe')),
    description TEXT,
    recommendation TEXT,
    source TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id_1) REFERENCES medications(medication_id) ON DELETE CASCADE,
    FOREIGN KEY (medication_id_2) REFERENCES medications(medication_id) ON DELETE CASCADE
);
```

---

### 8. Refill_Reminders Table

Tracks medication refill reminders and pharmacy information.

```sql
CREATE TABLE refill_reminders (
    refill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id INTEGER NOT NULL,
    pharmacy_name TEXT,
    pharmacy_phone TEXT,
    pharmacy_address TEXT,
    refill_date DATE,
    reminder_date DATE,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'ordered', 'completed', 'cancelled')),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE
);
```

---

## Relationships

### Primary Relationships

1. **Users ↔ Medications** (One-to-Many)
   - One user can have multiple medications
   - Each medication belongs to one user
   - Cascade delete: Deleting user removes all their medications

2. **Medications ↔ Reminders** (One-to-Many)
   - One medication can have multiple reminders
   - Each reminder is for one medication
   - Cascade delete: Deleting medication removes all its reminders

3. **Reminders ↔ Reminder_Logs** (One-to-Many)
   - One reminder can have multiple log entries
   - Each log entry belongs to one reminder
   - Cascade delete: Deleting reminder removes all its logs

4. **Medications ↔ Prescriptions** (One-to-Many)
   - One medication can have multiple prescriptions
   - Each prescription is for one medication

5. **Users ↔ User_Settings** (One-to-One)
   - Each user has one settings record
   - Each settings record belongs to one user

---

## Indexes

Strategic indexes to optimize query performance:

```sql
-- Users table indexes
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_is_active ON users(is_active);

-- Medications table indexes
CREATE INDEX idx_medications_user_id ON medications(user_id);
CREATE INDEX idx_medications_is_active ON medications(is_active);
CREATE INDEX idx_medications_category ON medications(category);
CREATE INDEX idx_medications_start_date ON medications(start_date);
CREATE INDEX idx_medications_end_date ON medications(end_date);
CREATE INDEX idx_medications_name ON medications(name);

-- Reminders table indexes
CREATE INDEX idx_reminders_medication_id ON reminders(medication_id);
CREATE INDEX idx_reminders_is_active ON reminders(is_active);
CREATE INDEX idx_reminders_time ON reminders(reminder_time);

-- Reminder_logs table indexes
CREATE INDEX idx_reminder_logs_reminder_id ON reminder_logs(reminder_id);
CREATE INDEX idx_reminder_logs_medication_id ON reminder_logs(medication_id);
CREATE INDEX idx_reminder_logs_status ON reminder_logs(status);
CREATE INDEX idx_reminder_logs_scheduled_time ON reminder_logs(scheduled_time);
CREATE INDEX idx_reminder_logs_created_at ON reminder_logs(created_at);

-- Prescriptions table indexes
CREATE INDEX idx_prescriptions_medication_id ON prescriptions(medication_id);
CREATE INDEX idx_prescriptions_user_id ON prescriptions(user_id);
CREATE INDEX idx_prescriptions_date ON prescriptions(prescription_date);

-- Refill_reminders table indexes
CREATE INDEX idx_refill_reminders_medication_id ON refill_reminders(medication_id);
CREATE INDEX idx_refill_reminders_status ON refill_reminders(status);
CREATE INDEX idx_refill_reminders_reminder_date ON refill_reminders(reminder_date);
```

---

## SQL Scripts

### Complete Database Creation Script

```sql
-- ==================================================
-- MedRemind Database Creation Script
-- Version: 1.0
-- Date: 2025-12-20
-- ==================================================

-- Enable foreign key constraints
PRAGMA foreign_keys = ON;

-- ==================================================
-- 1. Users Table
-- ==================================================
CREATE TABLE IF NOT EXISTS users (
    user_id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    date_of_birth DATE,
    phone_number TEXT,
    profile_image_path TEXT,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ==================================================
-- 2. Medications Table
-- ==================================================
CREATE TABLE IF NOT EXISTS medications (
    medication_id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    generic_name TEXT,
    dosage TEXT NOT NULL,
    dosage_unit TEXT DEFAULT 'mg',
    frequency TEXT NOT NULL,
    frequency_type TEXT DEFAULT 'daily',
    start_date DATE NOT NULL,
    end_date DATE,
    total_quantity INTEGER,
    remaining_quantity INTEGER,
    instructions TEXT,
    side_effects TEXT,
    category TEXT,
    color_code TEXT DEFAULT '#3498db',
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

-- ==================================================
-- 3. Reminders Table
-- ==================================================
CREATE TABLE IF NOT EXISTS reminders (
    reminder_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id INTEGER NOT NULL,
    reminder_time TIME NOT NULL,
    repeat_days TEXT DEFAULT '1,2,3,4,5,6,7',
    is_active BOOLEAN DEFAULT 1,
    snooze_count INTEGER DEFAULT 0,
    max_snooze INTEGER DEFAULT 3,
    advance_notification INTEGER DEFAULT 0,
    notification_enabled BOOLEAN DEFAULT 1,
    vibration_enabled BOOLEAN DEFAULT 1,
    sound_enabled BOOLEAN DEFAULT 1,
    custom_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE
);

-- ==================================================
-- 4. Reminder_Logs Table
-- ==================================================
CREATE TABLE IF NOT EXISTS reminder_logs (
    log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    reminder_id INTEGER NOT NULL,
    medication_id INTEGER NOT NULL,
    scheduled_time TIMESTAMP NOT NULL,
    status TEXT NOT NULL CHECK(status IN ('taken', 'skipped', 'missed', 'snoozed')),
    taken_at TIMESTAMP,
    skipped_at TIMESTAMP,
    missed_at TIMESTAMP,
    snoozed_at TIMESTAMP,
    snooze_until TIMESTAMP,
    notes TEXT,
    dosage_taken TEXT,
    location TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (reminder_id) REFERENCES reminders(reminder_id) ON DELETE CASCADE,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE
);

-- ==================================================
-- 5. Prescriptions Table
-- ==================================================
CREATE TABLE IF NOT EXISTS prescriptions (
    prescription_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    doctor_name TEXT,
    doctor_contact TEXT,
    hospital_name TEXT,
    prescription_date DATE NOT NULL,
    expiry_date DATE,
    prescription_number TEXT,
    image_path TEXT,
    pdf_path TEXT,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

-- ==================================================
-- 6. User_Settings Table
-- ==================================================
CREATE TABLE IF NOT EXISTS user_settings (
    setting_id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL UNIQUE,
    notification_sound TEXT DEFAULT 'default',
    notification_volume INTEGER DEFAULT 80,
    snooze_duration INTEGER DEFAULT 5,
    dark_mode BOOLEAN DEFAULT 0,
    language TEXT DEFAULT 'en',
    time_format TEXT DEFAULT '24h',
    date_format TEXT DEFAULT 'YYYY-MM-DD',
    auto_backup BOOLEAN DEFAULT 1,
    backup_frequency TEXT DEFAULT 'weekly',
    reminder_before_stock_ends INTEGER DEFAULT 7,
    pin_code TEXT,
    biometric_enabled BOOLEAN DEFAULT 0,
    sync_enabled BOOLEAN DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

-- ==================================================
-- 7. Medication_Interactions Table
-- ==================================================
CREATE TABLE IF NOT EXISTS medication_interactions (
    interaction_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id_1 INTEGER NOT NULL,
    medication_id_2 INTEGER NOT NULL,
    severity TEXT CHECK(severity IN ('minor', 'moderate', 'severe')),
    description TEXT,
    recommendation TEXT,
    source TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id_1) REFERENCES medications(medication_id) ON DELETE CASCADE,
    FOREIGN KEY (medication_id_2) REFERENCES medications(medication_id) ON DELETE CASCADE
);

-- ==================================================
-- 8. Refill_Reminders Table
-- ==================================================
CREATE TABLE IF NOT EXISTS refill_reminders (
    refill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    medication_id INTEGER NOT NULL,
    pharmacy_name TEXT,
    pharmacy_phone TEXT,
    pharmacy_address TEXT,
    refill_date DATE,
    reminder_date DATE,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'ordered', 'completed', 'cancelled')),
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (medication_id) REFERENCES medications(medication_id) ON DELETE CASCADE
);

-- ==================================================
-- Create Indexes
-- ==================================================

-- Users table indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);

-- Medications table indexes
CREATE INDEX IF NOT EXISTS idx_medications_user_id ON medications(user_id);
CREATE INDEX IF NOT EXISTS idx_medications_is_active ON medications(is_active);
CREATE INDEX IF NOT EXISTS idx_medications_category ON medications(category);
CREATE INDEX IF NOT EXISTS idx_medications_start_date ON medications(start_date);
CREATE INDEX IF NOT EXISTS idx_medications_end_date ON medications(end_date);
CREATE INDEX IF NOT EXISTS idx_medications_name ON medications(name);

-- Reminders table indexes
CREATE INDEX IF NOT EXISTS idx_reminders_medication_id ON reminders(medication_id);
CREATE INDEX IF NOT EXISTS idx_reminders_is_active ON reminders(is_active);
CREATE INDEX IF NOT EXISTS idx_reminders_time ON reminders(reminder_time);

-- Reminder_logs table indexes
CREATE INDEX IF NOT EXISTS idx_reminder_logs_reminder_id ON reminder_logs(reminder_id);
CREATE INDEX IF NOT EXISTS idx_reminder_logs_medication_id ON reminder_logs(medication_id);
CREATE INDEX IF NOT EXISTS idx_reminder_logs_status ON reminder_logs(status);
CREATE INDEX IF NOT EXISTS idx_reminder_logs_scheduled_time ON reminder_logs(scheduled_time);
CREATE INDEX IF NOT EXISTS idx_reminder_logs_created_at ON reminder_logs(created_at);

-- Prescriptions table indexes
CREATE INDEX IF NOT EXISTS idx_prescriptions_medication_id ON prescriptions(medication_id);
CREATE INDEX IF NOT EXISTS idx_prescriptions_user_id ON prescriptions(user_id);
CREATE INDEX IF NOT EXISTS idx_prescriptions_date ON prescriptions(prescription_date);

-- Refill_reminders table indexes
CREATE INDEX IF NOT EXISTS idx_refill_reminders_medication_id ON refill_reminders(medication_id);
CREATE INDEX IF NOT EXISTS idx_refill_reminders_status ON refill_reminders(status);
CREATE INDEX IF NOT EXISTS idx_refill_reminders_reminder_date ON refill_reminders(reminder_date);

-- ==================================================
-- Create Triggers for updated_at columns
-- ==================================================

-- Users trigger
CREATE TRIGGER IF NOT EXISTS update_users_timestamp 
AFTER UPDATE ON users
FOR EACH ROW
BEGIN
    UPDATE users SET updated_at = CURRENT_TIMESTAMP WHERE user_id = NEW.user_id;
END;

-- Medications trigger
CREATE TRIGGER IF NOT EXISTS update_medications_timestamp 
AFTER UPDATE ON medications
FOR EACH ROW
BEGIN
    UPDATE medications SET updated_at = CURRENT_TIMESTAMP WHERE medication_id = NEW.medication_id;
END;

-- Reminders trigger
CREATE TRIGGER IF NOT EXISTS update_reminders_timestamp 
AFTER UPDATE ON reminders
FOR EACH ROW
BEGIN
    UPDATE reminders SET updated_at = CURRENT_TIMESTAMP WHERE reminder_id = NEW.reminder_id;
END;

-- Prescriptions trigger
CREATE TRIGGER IF NOT EXISTS update_prescriptions_timestamp 
AFTER UPDATE ON prescriptions
FOR EACH ROW
BEGIN
    UPDATE prescriptions SET updated_at = CURRENT_TIMESTAMP WHERE prescription_id = NEW.prescription_id;
END;

-- User_settings trigger
CREATE TRIGGER IF NOT EXISTS update_user_settings_timestamp 
AFTER UPDATE ON user_settings
FOR EACH ROW
BEGIN
    UPDATE user_settings SET updated_at = CURRENT_TIMESTAMP WHERE setting_id = NEW.setting_id;
END;

-- Refill_reminders trigger
CREATE TRIGGER IF NOT EXISTS update_refill_reminders_timestamp 
AFTER UPDATE ON refill_reminders
FOR EACH ROW
BEGIN
    UPDATE refill_reminders SET updated_at = CURRENT_TIMESTAMP WHERE refill_id = NEW.refill_id;
END;

-- ==================================================
-- Create Views for Common Queries
-- ==================================================

-- View: Active medications with upcoming reminders
CREATE VIEW IF NOT EXISTS v_active_medications_with_reminders AS
SELECT 
    m.medication_id,
    m.user_id,
    m.name,
    m.dosage,
    m.dosage_unit,
    m.frequency,
    m.remaining_quantity,
    r.reminder_id,
    r.reminder_time,
    r.is_active as reminder_active
FROM medications m
LEFT JOIN reminders r ON m.medication_id = r.medication_id
WHERE m.is_active = 1
ORDER BY m.name, r.reminder_time;

-- View: Medication adherence summary
CREATE VIEW IF NOT EXISTS v_medication_adherence AS
SELECT 
    m.medication_id,
    m.name,
    m.user_id,
    COUNT(rl.log_id) as total_reminders,
    SUM(CASE WHEN rl.status = 'taken' THEN 1 ELSE 0 END) as taken_count,
    SUM(CASE WHEN rl.status = 'skipped' THEN 1 ELSE 0 END) as skipped_count,
    SUM(CASE WHEN rl.status = 'missed' THEN 1 ELSE 0 END) as missed_count,
    ROUND(CAST(SUM(CASE WHEN rl.status = 'taken' THEN 1 ELSE 0 END) AS FLOAT) / 
          NULLIF(COUNT(rl.log_id), 0) * 100, 2) as adherence_percentage
FROM medications m
LEFT JOIN reminder_logs rl ON m.medication_id = rl.medication_id
WHERE m.is_active = 1
GROUP BY m.medication_id, m.name, m.user_id;

-- View: Low stock medications
CREATE VIEW IF NOT EXISTS v_low_stock_medications AS
SELECT 
    m.medication_id,
    m.user_id,
    m.name,
    m.dosage,
    m.remaining_quantity,
    m.total_quantity,
    ROUND(CAST(m.remaining_quantity AS FLOAT) / NULLIF(m.total_quantity, 0) * 100, 2) as stock_percentage,
    us.reminder_before_stock_ends
FROM medications m
JOIN user_settings us ON m.user_id = us.user_id
WHERE m.is_active = 1
  AND m.remaining_quantity IS NOT NULL
  AND m.total_quantity IS NOT NULL
  AND (CAST(m.remaining_quantity AS FLOAT) / NULLIF(m.total_quantity, 0) * 100) <= 20
ORDER BY stock_percentage ASC;

-- ==================================================
-- Database initialization complete
-- ==================================================
```

---

## Data Types and Constraints

### SQLite Data Types Used

1. **INTEGER**: Used for IDs, counts, boolean values (0/1)
2. **TEXT**: Used for strings, names, descriptions
3. **DATE**: Stored as TEXT in 'YYYY-MM-DD' format
4. **TIME**: Stored as TEXT in 'HH:MM:SS' format
5. **TIMESTAMP**: Stored as TEXT in ISO8601 format
6. **BOOLEAN**: Stored as INTEGER (0 or 1)

### Constraints Applied

- **PRIMARY KEY**: Auto-incrementing unique identifiers
- **FOREIGN KEY**: Referential integrity with CASCADE delete
- **UNIQUE**: Ensures no duplicates for username, email
- **NOT NULL**: Required fields
- **CHECK**: Validates enum-like values
- **DEFAULT**: Default values for optional fields

---

## Sample Data

### Sample Insert Script

```sql
-- ==================================================
-- Sample Data for Testing
-- ==================================================

-- Insert sample user
INSERT INTO users (username, email, password_hash, first_name, last_name, date_of_birth, phone_number)
VALUES ('john_doe', 'john@example.com', '$2b$10$XYZ...', 'John', 'Doe', '1990-05-15', '+1234567890');

-- Insert user settings
INSERT INTO user_settings (user_id, notification_sound, snooze_duration, dark_mode, language)
VALUES (1, 'gentle_chime', 10, 1, 'en');

-- Insert sample medications
INSERT INTO medications (user_id, name, generic_name, dosage, dosage_unit, frequency, frequency_type, start_date, end_date, total_quantity, remaining_quantity, category, color_code)
VALUES 
(1, 'Amoxicillin', 'Amoxicillin', '500', 'mg', 'Three times daily', 'daily', '2025-12-01', '2025-12-15', 45, 30, 'Antibiotic', '#e74c3c'),
(1, 'Lisinopril', 'Lisinopril', '10', 'mg', 'Once daily', 'daily', '2025-12-01', NULL, 90, 75, 'Blood Pressure', '#3498db'),
(1, 'Metformin', 'Metformin HCl', '850', 'mg', 'Twice daily', 'daily', '2025-12-01', NULL, 180, 150, 'Diabetes', '#2ecc71');

-- Insert sample reminders
INSERT INTO reminders (medication_id, reminder_time, repeat_days, is_active)
VALUES 
(1, '08:00:00', '1,2,3,4,5,6,7', 1),
(1, '14:00:00', '1,2,3,4,5,6,7', 1),
(1, '20:00:00', '1,2,3,4,5,6,7', 1),
(2, '09:00:00', '1,2,3,4,5,6,7', 1),
(3, '08:30:00', '1,2,3,4,5,6,7', 1),
(3, '20:30:00', '1,2,3,4,5,6,7', 1);

-- Insert sample reminder logs
INSERT INTO reminder_logs (reminder_id, medication_id, scheduled_time, status, taken_at)
VALUES 
(1, 1, '2025-12-20 08:00:00', 'taken', '2025-12-20 08:05:00'),
(4, 2, '2025-12-20 09:00:00', 'taken', '2025-12-20 09:02:00'),
(5, 3, '2025-12-20 08:30:00', 'taken', '2025-12-20 08:35:00');

-- Insert sample prescription
INSERT INTO prescriptions (medication_id, user_id, doctor_name, doctor_contact, hospital_name, prescription_date, expiry_date, prescription_number)
VALUES 
(1, 1, 'Dr. Sarah Smith', '+1987654321', 'City General Hospital', '2025-12-01', '2026-12-01', 'RX123456');

-- Insert sample refill reminder
INSERT INTO refill_reminders (medication_id, pharmacy_name, pharmacy_phone, pharmacy_address, refill_date, reminder_date, status)
VALUES 
(2, 'HealthPlus Pharmacy', '+1555123456', '123 Main St, City', '2026-01-15', '2026-01-08', 'pending');
```

---

## Migration Scripts

### Version 1.0 to 1.1 (Future)

```sql
-- Example migration script structure
-- Uncomment and modify as needed for future schema changes

-- BEGIN TRANSACTION;

-- Add new columns
-- ALTER TABLE medications ADD COLUMN ndc_code TEXT;
-- ALTER TABLE medications ADD COLUMN barcode TEXT;

-- Create new tables
-- CREATE TABLE medication_history (
--     history_id INTEGER PRIMARY KEY AUTOINCREMENT,
--     medication_id INTEGER NOT NULL,
--     field_changed TEXT NOT NULL,
--     old_value TEXT,
--     new_value TEXT,
--     changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
--     FOREIGN KEY (medication_id) REFERENCES medications(medication_id)
-- );

-- Update schema version
-- CREATE TABLE IF NOT EXISTS schema_version (
--     version TEXT PRIMARY KEY,
--     applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
-- );
-- INSERT INTO schema_version (version) VALUES ('1.1');

-- COMMIT;
```

---

## Common Queries

### Useful SQL Queries for Application

```sql
-- 1. Get all active medications for a user
SELECT * FROM medications 
WHERE user_id = ? AND is_active = 1 
ORDER BY name;

-- 2. Get today's reminders for a user
SELECT m.name, m.dosage, m.dosage_unit, r.reminder_time
FROM medications m
JOIN reminders r ON m.medication_id = r.medication_id
WHERE m.user_id = ? 
  AND m.is_active = 1 
  AND r.is_active = 1
  AND CAST(strftime('%w', 'now') AS INTEGER) + 1 IN (
    SELECT value FROM (
      SELECT TRIM(value) as value 
      FROM (SELECT value FROM json_each('[' || r.repeat_days || ']'))
    )
  )
ORDER BY r.reminder_time;

-- 3. Get medication adherence for last 30 days
SELECT 
    m.name,
    COUNT(rl.log_id) as total,
    SUM(CASE WHEN rl.status = 'taken' THEN 1 ELSE 0 END) as taken,
    ROUND(CAST(SUM(CASE WHEN rl.status = 'taken' THEN 1 ELSE 0 END) AS FLOAT) / 
          COUNT(rl.log_id) * 100, 2) as adherence_rate
FROM medications m
JOIN reminder_logs rl ON m.medication_id = rl.medication_id
WHERE m.user_id = ? 
  AND rl.created_at >= date('now', '-30 days')
GROUP BY m.medication_id, m.name;

-- 4. Get medications running low on stock
SELECT 
    name, 
    remaining_quantity, 
    total_quantity,
    ROUND(CAST(remaining_quantity AS FLOAT) / total_quantity * 100, 2) as stock_percentage
FROM medications 
WHERE user_id = ? 
  AND is_active = 1
  AND remaining_quantity IS NOT NULL
  AND (CAST(remaining_quantity AS FLOAT) / total_quantity) < 0.25
ORDER BY stock_percentage ASC;

-- 5. Get missed medications
SELECT m.name, m.dosage, r.reminder_time, rl.scheduled_time
FROM reminder_logs rl
JOIN reminders r ON rl.reminder_id = r.reminder_id
JOIN medications m ON rl.medication_id = m.medication_id
WHERE m.user_id = ? 
  AND rl.status = 'missed'
  AND DATE(rl.scheduled_time) = DATE('now')
ORDER BY rl.scheduled_time;
```

---

## Database Maintenance

### Backup Script

```sql
-- Export database to SQL file
.output backup_medremind_YYYYMMDD.sql
.dump
.output stdout
```

### Vacuum and Optimize

```sql
-- Reclaim unused space and optimize database
VACUUM;
ANALYZE;
```

### Check Database Integrity

```sql
-- Check for database corruption
PRAGMA integrity_check;

-- Check foreign key constraints
PRAGMA foreign_key_check;
```

---

## Security Considerations

1. **Password Hashing**: All passwords must be hashed using bcrypt or Argon2
2. **SQL Injection Prevention**: Use parameterized queries always
3. **Encryption**: Consider encrypting the entire database file using SQLCipher
4. **Backup Encryption**: Encrypt backup files before cloud storage
5. **Access Control**: Implement proper authentication and authorization
6. **Data Sanitization**: Validate and sanitize all user inputs

---

## Performance Optimization Tips

1. **Use Indexes**: Already created for frequently queried columns
2. **Limit Result Sets**: Use LIMIT clause for large queries
3. **Avoid SELECT ***: Query only needed columns
4. **Use Transactions**: Batch multiple INSERTs/UPDATEs in transactions
5. **Regular VACUUM**: Run VACUUM periodically to reclaim space
6. **Analyze Statistics**: Run ANALYZE after significant data changes

---

## Database File Location

### Android
```
/data/data/com.yourapp.medremind/databases/medremind.db
```

### iOS
```
~/Library/Application Support/MedRemind/medremind.db
```

### Desktop
```
~/.medremind/medremind.db
```

---

## Support and Maintenance

For database-related issues or questions:
- Check SQLite documentation: https://www.sqlite.org/docs.html
- Review application logs for SQL errors
- Test queries in SQLite browser before implementing
- Maintain regular backups before schema changes

---

## Version History

| Version | Date       | Changes                          |
|---------|------------|----------------------------------|
| 1.0     | 2025-12-20 | Initial database schema created  |

---

## Appendix

### A. Database Size Estimates

- Empty database: ~100 KB
- 10 medications + 1 year logs: ~5 MB
- 50 medications + 1 year logs: ~20 MB
- 100 medications + 1 year logs: ~40 MB

### B. Recommended Maintenance Schedule

- **Daily**: Automatic backups
- **Weekly**: Check for missed reminders cleanup
- **Monthly**: VACUUM and ANALYZE
- **Quarterly**: Integrity check
- **Yearly**: Archive old logs

---

**End of Database Schema Documentation**
