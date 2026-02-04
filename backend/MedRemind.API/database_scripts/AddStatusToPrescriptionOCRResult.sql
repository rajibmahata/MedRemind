-- Migration: Add Status field to PrescriptionOCRResult table

-- 1. Add Status column with default value
ALTER TABLE PrescriptionOCRResults
ADD Status INTEGER NOT NULL DEFAULT 1;

-- 2. Update existing records to "Processed" status (3)
UPDATE PrescriptionOCRResults
SET Status = 3
WHERE Status = 1;

-- 3. Add index on Status for filtering
CREATE INDEX IX_PrescriptionOCRResults_Status 
    ON PrescriptionOCRResults (Status);

-- 4. Add column description
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Processing status: 1=Processing, 2=OcrComplete, 3=Processed, 4=Failed, 5=Duplicate', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'PrescriptionOCRResults',
    @level2type = N'COLUMN', @level2name = 'Status';

-- Status Enum Values:
-- 1 = Processing (OCR in progress)
-- 2 = OcrComplete (OCR done, AI processing next)
-- 3 = Processed (Complete)
-- 4 = Failed (Error occurred)
-- 5 = Duplicate (Duplicate prescription detected)
