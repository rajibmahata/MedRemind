-- Migration: Add Age-Related Safety Fields to Medications Table

-- 1. Add AgeAppropriate column (nullable boolean)
ALTER TABLE Medications
ADD AgeAppropriate INTEGER NULL;

-- 2. Add AgeSpecificWarning column (nullable text)
ALTER TABLE Medications
ADD AgeSpecificWarning TEXT NULL;

-- 3. Add column descriptions
-- SQLite doesn't support sp_addextendedproperty, but we document here:
-- AgeAppropriate: Whether the medication dosage is appropriate for the patient's age (true/false/null)
-- AgeSpecificWarning: Age-specific warnings or precautions for this medication

-- Note: These fields are populated by the Python middleware CrewAI LLM validation
-- Values come from Python response fields: age_appropriate and age_specific_warning
