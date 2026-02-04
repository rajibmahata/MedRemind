-- Migration: Add Medicine Details and Side Effects to Medication table
-- Add LLM Metadata to PrescriptionOCRResult table

-- 1. Add new columns to Medication table
ALTER TABLE Medications
ADD MedicineDetails NVARCHAR(MAX) NULL;

ALTER TABLE Medications
ADD SideEffects NVARCHAR(MAX) NULL;

-- 2. Add new columns to PrescriptionOCRResult table
ALTER TABLE PrescriptionOCRResults
ADD DeepSeekResponse NVARCHAR(MAX) NULL;

ALTER TABLE PrescriptionOCRResults
ADD PythonMiddlewareVersion NVARCHAR(50) NULL;

ALTER TABLE PrescriptionOCRResults
ADD LlmModelsUsed NVARCHAR(500) NULL;

ALTER TABLE PrescriptionOCRResults
ADD CrewAISummary NVARCHAR(MAX) NULL;

ALTER TABLE PrescriptionOCRResults
ADD OverallSafetyScore FLOAT NULL;

ALTER TABLE PrescriptionOCRResults
ADD RequiresPharmacistReview BIT NULL;

ALTER TABLE PrescriptionOCRResults
ADD SafetyWarningsCount INT NULL;

ALTER TABLE PrescriptionOCRResults
ADD DrugInteractionsCount INT NULL;

-- 3. Update description of existing columns to reflect new usage
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'JSON response from Python middleware using OpenAI LLM', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'PrescriptionOCRResults',
    @level2type = N'COLUMN', @level2name = 'OpenAIResponse';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'JSON medicine validation from Python middleware using Claude LLM', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'PrescriptionOCRResults',
    @level2type = N'COLUMN', @level2name = 'ClaudeResponse';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Indicates Python Middleware (CrewAI) with multiple LLM APIs', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE',  @level1name = 'PrescriptionOCRResults',
    @level2type = N'COLUMN', @level2name = 'SelectedProvider';
 purpose: Optional[str] = Field(default=None, description="Medical condition or purpose for this medication")
    side_effects: Optional[List[str]] = Field(default=None, description="Common side effects of this medication")
   