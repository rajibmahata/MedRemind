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


# ============================================
# OCR Extraction Models
# ============================================

class OCRRequest(BaseModel):
    """Request for OCR extraction from base64 document"""
    document_base64: str = Field(..., description="Base64 encoded document (image or PDF)")
    document_type: str = Field(default="auto", description="Document type: 'image', 'pdf', or 'auto' (auto-detect)")
    language_hints: Optional[List[str]] = Field(default=None, description="Language hints (e.g., ['en', 'hi', 'bn'])")
    prescription_id: Optional[str] = Field(default=None, description="Optional prescription identifier")
    enhance_handwriting: bool = Field(default=True, description="Enable enhanced handwriting recognition")
    extract_structured_data: bool = Field(default=False, description="Also extract structured prescription data")


class OCRConfidence(BaseModel):
    """Confidence scores for OCR extraction"""
    overall: float = Field(ge=0.0, le=1.0, description="Overall confidence score")
    text_clarity: float = Field(ge=0.0, le=1.0, description="Text clarity score")
    handwriting_quality: float = Field(ge=0.0, le=1.0, description="Handwriting readability score")
    language_confidence: float = Field(ge=0.0, le=1.0, description="Language detection confidence")


class DetectedLanguage(BaseModel):
    """Detected language information"""
    code: str = Field(description="ISO 639-1 language code")
    name: str = Field(description="Language name")
    confidence: float = Field(ge=0.0, le=1.0)


class TextRegion(BaseModel):
    """A region of text detected in the document"""
    text: str
    region_type: Optional[str] = Field(default=None, description="Type: header, body, footer, signature, etc.")
    confidence: float = Field(ge=0.0, le=1.0)
    bounding_box: Optional[List[int]] = Field(default=None, description="[x, y, width, height]")


class OCRResponse(BaseModel):
    """Response from OCR extraction"""
    success: bool
    prescription_id: Optional[str] = None
    
    # Raw extracted text
    raw_text: str = Field(description="Complete extracted text from document")
    
    # Structured regions (if detected)
    text_regions: Optional[List[TextRegion]] = Field(default=None, description="Detected text regions")
    
    # Language information
    detected_languages: List[DetectedLanguage] = Field(default=[], description="Detected languages")
    primary_language: Optional[str] = Field(default=None, description="Primary detected language")
    
    # Quality metrics
    confidence: OCRConfidence
    
    # Document metadata
    document_type: str = Field(description="Detected document type")
    page_count: int = Field(default=1, description="Number of pages processed")
    
    # Processing info
    processing_time: float
    ocr_provider: str = Field(description="OCR provider used")
    
    # Optional structured data (if extract_structured_data=True)
    structured_data: Optional[ParseResponse] = Field(default=None, description="Extracted prescription data")
    
    # Errors/warnings
    warnings: List[str] = Field(default=[])
    error_message: Optional[str] = None


class OCRProviderConfig(BaseModel):
    """Configuration for OCR providers"""
    provider: str = Field(description="Provider name: 'openai_vision', 'azure_di', 'google_vision', 'tesseract'")
    enabled: bool = True
    priority: int = Field(default=1, description="Priority order (lower = higher priority)")
    fallback: bool = Field(default=True, description="Use as fallback if primary fails")
