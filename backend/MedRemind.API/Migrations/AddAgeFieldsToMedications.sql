-- Migration: Add Age-Related and Safety Validation Fields to Medications Table
-- This migration adds 8 new fields to track per-medication safety information from Python CrewAI validator

-- =====================================================
-- AGE-RELATED SAFETY FIELDS
-- =====================================================

-- 1. Add AgeAppropriate column (nullable boolean)
ALTER TABLE Medications
ADD AgeAppropriate INTEGER NULL;

-- 2. Add AgeSpecificWarning column (nullable text)
ALTER TABLE Medications
ADD AgeSpecificWarning TEXT NULL;

-- =====================================================
-- SAFETY VALIDATION FIELDS (from CrewAI Safety Agent)
-- =====================================================

-- 3. Add SafetyWarningType column (nullable text)
-- Types: age, dosage, frequency, duration, monitoring
ALTER TABLE Medications
ADD SafetyWarningType TEXT NULL;

-- 4. Add SafetyWarningSeverity column (nullable text)
-- Severity: low, medium, high, critical
ALTER TABLE Medications
ADD SafetyWarningSeverity TEXT NULL;

-- 5. Add SafetyWarningMessage column (nullable text)
-- The actual warning message for this medication
ALTER TABLE Medications
ADD SafetyWarningMessage TEXT NULL;

-- 6. Add SafetyWarningRecommendation column (nullable text)
-- Recommended action for this warning
ALTER TABLE Medications
ADD SafetyWarningRecommendation TEXT NULL;

-- 7. Add SafetyScore column (nullable real for double)
-- Individual medication safety score (0.0 to 1.0)
ALTER TABLE Medications
ADD SafetyScore REAL NULL;

-- 8. Add RequiresPharmacistReview column (boolean, default false)
-- Whether this medication requires pharmacist review
ALTER TABLE Medications
ADD RequiresPharmacistReview INTEGER NOT NULL DEFAULT 0;

-- =====================================================
-- COLUMN DESCRIPTIONS
-- =====================================================

-- SQLite doesn't support sp_addextendedproperty, but we document here:

-- AgeAppropriate: 
--   Whether the medication dosage is appropriate for the patient's age (true/false/null)

-- AgeSpecificWarning: 
--   Age-specific warnings or precautions for this medication

-- SafetyWarningType: 
--   Type of safety warning (age, dosage, frequency, duration, monitoring)

-- SafetyWarningSeverity: 
--   Severity level (low, medium, high, critical)

-- SafetyWarningMessage: 
--   The actual warning message for this medication

-- SafetyWarningRecommendation: 
--   Recommended action for this warning

-- SafetyScore: 
--   Individual medication safety score (0.0 to 1.0)

-- RequiresPharmacistReview: 
--   Whether this medication requires pharmacist review (true/false)

-- =====================================================
-- DATA SOURCE
-- =====================================================

-- These fields are populated by the Python middleware CrewAI LLM validation
-- Source: Python response fields from medicine_validation.safety_warnings
-- The safety warning is matched to the medication by name

-- =====================================================
-- ARCHITECTURE NOTE
-- =====================================================

-- PRESCRIPTION-LEVEL DATA (stored in PrescriptionOCRResults table):
--   - drug_interactions (JSON array)
--   - All safety_warnings (JSON array)
--   - duplicate_therapies (JSON array)
--   - overall_safety_score
--   - requires_pharmacist_review

-- MEDICATION-LEVEL DATA (stored in Medications table):
--   - Individual safety warning for THIS medication
--   - Age appropriateness for THIS medication
--   - Safety score for THIS medication
--   - Pharmacist review flag for THIS medication

-- RATIONALE: 
-- Drug interactions and duplicate therapies involve multiple medications,
-- so they are stored at prescription level. Individual medication warnings
-- are stored with each medication for easy access and display.
