"""
Pydantic models for API requests and responses
"""

from datetime import datetime
from typing import List, Optional
from pydantic import BaseModel, Field


class PatientInfo(BaseModel):
    """Patient information"""
    name: Optional[str] = None
    age: Optional[int] = None
    gender: Optional[str] = None


class DoctorInfo(BaseModel):
    """Doctor information"""
    name: Optional[str] = None
    specialization: Optional[str] = None
    registration_number: Optional[str] = None


class Medication(BaseModel):
    """Medication details"""
    name: str
    dosage: Optional[str] = None
    unit: Optional[str] = None
    frequency: Optional[str] = None
    frequency_count: Optional[int] = None
    duration: Optional[str] = None
    duration_days: Optional[int] = None
    timing: Optional[str] = None
    instructions: Optional[str] = None
    purpose: Optional[str] = Field(default=None, description="Medical condition or purpose for this medication")
    side_effects: Optional[List[str]] = Field(default=None, description="Common side effects of this medication")
    age_appropriate: Optional[bool] = Field(default=None, description="Whether dosage is appropriate for patient age")
    age_specific_warning: Optional[str] = Field(default=None, description="Age-specific warnings or precautions")
    confidence_score: float = Field(default=0.0, ge=0.0, le=1.0)


class DrugInteraction(BaseModel):
    """Drug interaction details"""
    medicines: List[str]
    severity: str  # minor, moderate, major, contraindicated
    description: str
    recommendation: Optional[str] = None


class SafetyWarning(BaseModel):
    """Safety warning for medication"""
    medicine: str
    type: str  # dosage, frequency, duration, monitoring, age
    severity: str  # low, medium, high, critical
    message: str
    recommendation: Optional[str] = None


class DuplicateTherapy(BaseModel):
    """Duplicate therapy detection"""
    medicines: List[str]
    therapeutic_class: str
    recommendation: Optional[str] = None


class MedicineValidation(BaseModel):
    """Medicine validation results"""
    drug_interactions: List[DrugInteraction] = []
    safety_warnings: List[SafetyWarning] = []
    duplicate_therapies: List[DuplicateTherapy] = []
    overall_safety_score: float = Field(default=1.0, ge=0.0, le=1.0)
    requires_pharmacist_review: bool = False


class ParseRequest(BaseModel):
    """Request to parse prescription"""
    ocr_text: str = Field(..., description="OCR extracted text from prescription")
    prescription_id: str = Field(..., description="Unique prescription identifier")
    save_result: bool = Field(default=True, description="Save parsed result to file")


class ParseResponse(BaseModel):
    """Response from prescription parsing"""
    success: bool
    prescription_id: str
    patient: Optional[PatientInfo] = None
    doctor: Optional[DoctorInfo] = None
    prescription_date: Optional[str] = None
    medications: List[Medication] = []
    medicine_validation: Optional[MedicineValidation] = None
    warnings: List[str] = []
    processing_time: float
    crew_summary: Optional[str] = None
    error_message: Optional[str] = None


class HealthResponse(BaseModel):
    """Health check response"""
    status: str
    version: str
    timestamp: datetime
    enabled_parsers: List[str]
