-- Migration: Remove AiResponse and ConfidenceScore, Add FileName and FileSize to Prescription table

-- 1. Add new columns
ALTER TABLE Prescriptions
ADD FileName NVARCHAR(500) NULL;

ALTER TABLE Prescriptions
ADD FileSize BIGINT NULL;

-- 2. Remove old columns (after backing up data if needed)
ALTER TABLE Prescriptions
DROP COLUMN AiResponse;

ALTER TABLE Prescriptions
DROP COLUMN ConfidenceScore;

-- 3. Add column descriptions
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Original or unique file name of the uploaded prescription image', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Prescriptions',
    @level2type = N'COLUMN', @level2name = 'FileName';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'File size in bytes of the uploaded prescription image', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'Prescriptions',
    @level2type = N'COLUMN', @level2name = 'FileSize';

-- 4. Optional: Update existing records with file size from ImagePath if needed
-- UPDATE Prescriptions SET FileName = ImagePath WHERE FileName IS NULL;
