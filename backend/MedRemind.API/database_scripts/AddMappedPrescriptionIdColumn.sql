-- Migration: Add MappedPrescriptionId to Prescriptions Table
-- This column stores the original prescription ID when a duplicate is detected
-- Only populated for prescriptions with Status = 'Duplicate'

-- Add MappedPrescriptionId column (nullable integer)
ALTER TABLE Prescriptions
ADD MappedPrescriptionId INTEGER NULL;

-- Add index for performance when querying duplicates
CREATE INDEX IF NOT EXISTS IX_Prescriptions_MappedPrescriptionId 
ON Prescriptions(MappedPrescriptionId);

-- Add index for combined Status + MappedPrescriptionId queries
CREATE INDEX IF NOT EXISTS IX_Prescriptions_Status_MappedPrescriptionId 
ON Prescriptions(Status, MappedPrescriptionId);

-- Verification query (optional - can be removed)
-- SELECT * FROM pragma_table_info('Prescriptions') WHERE name = 'MappedPrescriptionId';
