"""
Prescription Processing Crew
Orchestrates sequential execution of agents
"""

import json
import time
from datetime import datetime
from crewai import Crew, Task
from langchain_openai import ChatOpenAI
from langchain_anthropic import ChatAnthropic

from app.config import settings
from app.agents.normalizer import create_normalizer_agent, normalize_ocr_text
from app.agents.extractor import create_extractor_agent, EXTRACTION_PROMPT
from app.agents.validator import create_validator_agent, VALIDATION_PROMPT
from app.agents.medicine_validator import create_medicine_validator_agent, MEDICINE_VALIDATION_PROMPT
from app.models import (
    ParseResponse, PatientInfo, DoctorInfo, Medication,
    DrugInteraction, SafetyWarning, DuplicateTherapy, MedicineValidation
)


def get_llm():
    """Get configured LLM based on settings"""
    if settings.preferred_parser == "openai" and settings.openai_enabled:
        return ChatOpenAI(
            model=settings.openai_model,
            api_key=settings.openai_api_key,
            temperature=0.0
        )
    elif settings.preferred_parser == "claude" and settings.claude_enabled:
        return ChatAnthropic(
            model=settings.claude_model,
            api_key=settings.claude_api_key,
            temperature=0.0
        )
    else:
        # Default to OpenAI
        return ChatOpenAI(
            model=settings.openai_model,
            api_key=settings.openai_api_key,
            temperature=0.0
        )


class PrescriptionCrew:
    """CrewAI crew for prescription processing"""
    
    def __init__(self):
        self.llm = get_llm()
        self.normalizer = create_normalizer_agent(self.llm)
        self.extractor = create_extractor_agent(self.llm)
        self.validator = create_validator_agent(self.llm)
        self.medicine_validator = create_medicine_validator_agent(self.llm)
    
    async def process(self, ocr_text: str, prescription_id: str) -> ParseResponse:
        """
        Process prescription through CrewAI agents
        
        Args:
            ocr_text: Raw OCR text
            prescription_id: Unique prescription identifier
            
        Returns:
            ParseResponse with extracted data
        """
        start_time = time.time()
        
        try:
            # Step 1: Pre-normalize OCR text (reduce token usage)
            normalized_text = normalize_ocr_text(ocr_text)
            print(f"\n?? Normalized text ({len(normalized_text)} chars)")
            
            # Step 2: Create extraction task
            extraction_task = Task(
                description=EXTRACTION_PROMPT.format(normalized_text=normalized_text),
                expected_output="Valid JSON with extracted prescription data",
                agent=self.extractor
            )
            
            # Step 3: Execute extraction
            print("\n?? Starting CrewAI extraction...")
            crew = Crew(
                agents=[self.extractor],
                tasks=[extraction_task],
                verbose=True
            )
            
            result = crew.kickoff()
            
            # Parse extraction result
            extracted_data = self._parse_extraction_result(result)
            print(f"\n? Extracted: {len(extracted_data.get('medications', []))} medications")
            
            # Step 4: Validate medicines
            medicine_validation = self._validate_medicines(extracted_data)
            
            # Step 5: General validation
            validation_result = self._validate_data(extracted_data)
            warnings = validation_result.get('warnings', [])
            
            # Add medicine validation warnings
            warnings.extend(medicine_validation.get('warnings', []))
            
            # Step 5: Build response
            response = self._build_response(
                extracted_data,
                prescription_id,
                warnings,
                medicine_validation,
                time.time() - start_time
            )
            
            print(f"\n?? Crew processing complete in {response.processing_time:.2f}s")
            return response
            
        except Exception as e:
            print(f"\n? Crew processing failed: {str(e)}")
            return ParseResponse(
                success=False,
                prescription_id=prescription_id,
                error_message=str(e),
                processing_time=time.time() - start_time
            )
    
    def _parse_extraction_result(self, result) -> dict:
        """Parse CrewAI result into dictionary"""
        try:
            # CrewAI returns result as string
            result_str = str(result)
            
            # Try to extract JSON from markdown code blocks
            if "```json" in result_str:
                json_start = result_str.find("```json") + 7
                json_end = result_str.find("```", json_start)
                json_str = result_str[json_start:json_end].strip()
            elif "```" in result_str:
                json_start = result_str.find("```") + 3
                json_end = result_str.find("```", json_start)
                json_str = result_str[json_start:json_end].strip()
            else:
                json_str = result_str
            
            return json.loads(json_str)
        except json.JSONDecodeError as e:
            print(f"?? JSON parse error: {e}")
            print(f"Raw result: {result_str[:500]}")
            return {
                "patient": None,
                "doctor": None,
                "prescription_date": None,
                "medications": []
            }
    
    def _validate_medicines(self, extracted_data: dict) -> dict:
        """Validate medicines using the medicine validator agent"""
        try:
            medications = extracted_data.get('medications', [])
            patient = extracted_data.get('patient', {})
            
            if not medications:
                return {
                    "validated_medications": [],
                    "drug_interactions": [],
                    "safety_warnings": [],
                    "duplicate_therapies": [],
                    "overall_safety_score": 1.0,
                    "requires_pharmacist_review": False,
                    "warnings": []
                }
            
            # Prepare data for medicine validation
            medications_json = json.dumps(medications, indent=2)
            patient_info = json.dumps({
                "name": patient.get('name'),
                "age": patient.get('age'),
                "gender": patient.get('gender')
            }, indent=2)
            
            # Create validation task
            validation_task = Task(
                description=MEDICINE_VALIDATION_PROMPT.format(
                    medications_json=medications_json,
                    patient_info=patient_info
                ),
                expected_output="Valid JSON with medicine validation results",
                agent=self.medicine_validator
            )
            
            # Execute validation
            print("\n?? Validating medicines...")
            crew = Crew(
                agents=[self.medicine_validator],
                tasks=[validation_task],
                verbose=True
            )
            
            result = crew.kickoff()
            validation_result = self._parse_extraction_result(result)
            
            # Build warnings list from validation
            warnings = []
            
            # Add drug interaction warnings
            for interaction in validation_result.get('drug_interactions', []):
                severity = interaction.get('severity', 'unknown')
                medicines = ', '.join(interaction.get('medicines', []))
                warnings.append(
                    f"Drug Interaction ({severity}): {medicines} - {interaction.get('description', '')}"
                )
            
            # Add safety warnings
            for warning in validation_result.get('safety_warnings', []):
                severity = warning.get('severity', 'unknown')
                medicine = warning.get('medicine', 'Unknown')
                warnings.append(
                    f"Safety Warning ({severity}): {medicine} - {warning.get('message', '')}"
                )
            
            # Add duplicate therapy warnings
            for dup in validation_result.get('duplicate_therapies', []):
                medicines = ', '.join(dup.get('medicines', []))
                warnings.append(
                    f"Duplicate Therapy: {medicines} ({dup.get('therapeutic_class', 'same class')})"
                )
            
            validation_result['warnings'] = warnings
            
            print(f"?? Medicine validation complete: {len(warnings)} warnings")
            return validation_result
            
        except Exception as e:
            print(f"?? Medicine validation error: {str(e)}")
            return {
                "validated_medications": [],
                "drug_interactions": [],
                "safety_warnings": [],
                "duplicate_therapies": [],
                "overall_safety_score": 0.5,
                "requires_pharmacist_review": True,
                "warnings": [f"Medicine validation error: {str(e)}"]
            }
    
    def _validate_data(self, extracted_data: dict) -> dict:
        """Validate extracted data"""
        warnings = []
        
        # Check required fields
        if not extracted_data.get('patient'):
            warnings.append("Patient information missing")
        
        if not extracted_data.get('doctor'):
            warnings.append("Doctor information missing")
        
        if not extracted_data.get('prescription_date'):
            warnings.append("Prescription date missing")
        
        meds = extracted_data.get('medications', [])
        if not meds:
            warnings.append("No medications found")
        
        # Check medication details
        for med in meds:
            if not med.get('name'):
                warnings.append("Medication with missing name")
            
            if med.get('frequency_count', 0) > 6:
                warnings.append(f"{med.get('name')}: Excessive frequency ({med['frequency_count']}x daily)")
            
            if med.get('duration_days', 0) > 365:
                warnings.append(f"{med.get('name')}: Very long duration ({med['duration_days']} days)")
        
        return {
            "is_valid": len(warnings) == 0 or all('missing' in w.lower() for w in warnings),
            "warnings": warnings,
            "errors": []
        }
    
    def _build_response(
        self,
        extracted_data: dict,
        prescription_id: str,
        warnings: list,
        medicine_validation: dict,
        processing_time: float
    ) -> ParseResponse:
        """Build ParseResponse from extracted data"""
        
        # Parse patient
        patient_data = extracted_data.get('patient', {})
        patient = PatientInfo(
            name=patient_data.get('name'),
            age=patient_data.get('age'),
            gender=patient_data.get('gender')
        ) if patient_data else None
        
        # Parse doctor
        doctor_data = extracted_data.get('doctor', {})
        doctor = DoctorInfo(
            name=doctor_data.get('name'),
            specialization=doctor_data.get('specialization'),
            registration_number=doctor_data.get('registration_number')
        ) if doctor_data else None
        
        # Parse medications
        medications = [
            Medication(
                name=med.get('name', 'Unknown'),
                dosage=med.get('dosage'),
                unit=med.get('unit'),
                frequency=med.get('frequency'),
                frequency_count=med.get('frequency_count'),
                duration=med.get('duration'),
                duration_days=med.get('duration_days'),
                timing=med.get('timing'),
                instructions=med.get('instructions'),
                confidence_score=med.get('confidence_score', 0.0)
            )
            for med in extracted_data.get('medications', [])
        ]
        
        # Build medicine validation object
        med_validation = None
        if medicine_validation:
            med_validation = MedicineValidation(
                drug_interactions=[
                    DrugInteraction(
                        medicines=interaction.get('medicines', []),
                        severity=interaction.get('severity', 'unknown'),
                        description=interaction.get('description', ''),
                        recommendation=interaction.get('recommendation')
                    )
                    for interaction in medicine_validation.get('drug_interactions', [])
                ],
                safety_warnings=[
                    SafetyWarning(
                        medicine=warning.get('medicine', 'Unknown'),
                        type=warning.get('type', 'general'),
                        severity=warning.get('severity', 'unknown'),
                        message=warning.get('message', ''),
                        recommendation=warning.get('recommendation')
                    )
                    for warning in medicine_validation.get('safety_warnings', [])
                ],
                duplicate_therapies=[
                    DuplicateTherapy(
                        medicines=dup.get('medicines', []),
                        therapeutic_class=dup.get('therapeutic_class', ''),
                        recommendation=dup.get('recommendation')
                    )
                    for dup in medicine_validation.get('duplicate_therapies', [])
                ],
                overall_safety_score=medicine_validation.get('overall_safety_score', 1.0),
                requires_pharmacist_review=medicine_validation.get('requires_pharmacist_review', False)
            )
        
        return ParseResponse(
            success=True,
            prescription_id=prescription_id,
            patient=patient,
            doctor=doctor,
            prescription_date=extracted_data.get('prescription_date'),
            medications=medications,
            medicine_validation=med_validation,
            warnings=warnings,
            processing_time=processing_time,
            crew_summary=f"Processed {len(medications)} medication(s) successfully"
        )


# Global crew instance
_crew_instance = None


def get_crew() -> PrescriptionCrew:
    """Get or create crew instance"""
    global _crew_instance
    if _crew_instance is None:
        _crew_instance = PrescriptionCrew()
    return _crew_instance
