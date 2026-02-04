-- Script to manually create PrescriptionOCRResults table
-- Run this if migration fails due to existing tables

CREATE TABLE IF NOT EXISTS "PrescriptionOCRResults" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PrescriptionOCRResults" PRIMARY KEY AUTOINCREMENT,
    "PrescriptionId" INTEGER NOT NULL,
    "OCRText" TEXT NOT NULL,
    "OCRTextHash" TEXT NOT NULL,
    "OpenAIResponse" TEXT NULL,
    "ClaudeResponse" TEXT NULL,
    "SelectedResponse" TEXT NULL,
    "SelectedProvider" TEXT NULL,
    "ComparisonScore" REAL NOT NULL,
    "ComparisonReason" TEXT NULL,
    "MedicationCount" INTEGER NOT NULL,
    "DoctorName" TEXT NULL,
    "PatientName" TEXT NULL,
    "PrescriptionDate" TEXT NULL,
    "ProcessedAt" TEXT NOT NULL,
    "ProcessingTime" TEXT NOT NULL,
    "ProcessingAttempts" INTEGER NOT NULL,
    CONSTRAINT "FK_PrescriptionOCRResults_Prescriptions_PrescriptionId" 
        FOREIGN KEY ("PrescriptionId") 
        REFERENCES "Prescriptions" ("Id") 
        ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_PrescriptionOCRResults_OCRTextHash" 
    ON "PrescriptionOCRResults" ("OCRTextHash");

CREATE INDEX IF NOT EXISTS "IX_PrescriptionOCRResults_PrescriptionId" 
    ON "PrescriptionOCRResults" ("PrescriptionId");
