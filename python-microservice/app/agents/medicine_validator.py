"""
Agent 4: Medicine Validation Agent
Role: Validate medicine names, dosages, and interactions
Goal: Ensure medicines are correctly identified and safe
"""

from crewai import Agent


def create_medicine_validator_agent(llm) -> Agent:
    """Create the medicine validation agent"""
    return Agent(
        role="Clinical Pharmacist & Medicine Validator",
        goal="Validate medicine names, dosages, and identify potential drug interactions",
        backstory="""You are a licensed clinical pharmacist with extensive knowledge of:
        - Generic and brand names of medicines
        - Standard dosage ranges for common medications
        - Drug-drug interactions and contraindications
        - Common prescription errors and red flags
        
        Your expertise includes:
        1. Verifying medicine names (correcting OCR errors)
        2. Validating dosages are within therapeutic ranges
        3. Identifying potential drug interactions
        4. Flagging unusual or dangerous combinations
        5. Recognizing duplicate therapies (same drug class)
        
        You provide detailed warnings but don't reject prescriptions outright.""",
        llm=llm,
        verbose=True,
        allow_delegation=False
    )


MEDICINE_VALIDATION_PROMPT = """Validate the following medications for accuracy, safety, and interactions.

VALIDATION CHECKS:
1. **Medicine Name Validation**
   - Verify each medicine name is correct (check for OCR errors)
   - Suggest corrections for misspelled medicine names
   - Identify if it's generic or brand name

2. **Dosage Validation**
   - Check if dosage is within therapeutic range
   - Flag if dosage is unusually high or low
   - Verify units are appropriate (mg, mcg, IU, etc.)

3. **Drug Interactions**
   - Identify potential drug-drug interactions
   - Flag contraindicated combinations
   - Note if medicines from same class (duplicate therapy)

4. **Safety Concerns**
   - Extremely high doses
   - Medications that require monitoring
   - Age-inappropriate medications (if patient age provided)
   - Frequency exceeding maximum daily dose

Medications to validate:
{medications_json}

Patient Info:
{patient_info}

Return ONLY valid JSON in this exact format:
{{
  "validated_medications": [
    {{
      "original_name": "string",
      "corrected_name": "string (if different from original)",
      "is_valid_name": true/false,
      "dosage_status": "normal|low|high|excessive",
      "dosage_warning": "string or null",
      "generic_name": "string",
      "brand_names": ["list of brand names"],
      "therapeutic_class": "string",
      "purpose": "string - what this medicine is used for/indication",
      "side_effects": ["list of common side effects"]
    }}
  ],
  "drug_interactions": [
    {{
      "medicines": ["medicine1", "medicine2"],
      "severity": "minor|moderate|major|contraindicated",
      "description": "string",
      "recommendation": "string"
    }}
  ],
  "safety_warnings": [
    {{
      "medicine": "string",
      "type": "dosage|frequency|duration|monitoring|age",
      "severity": "low|medium|high|critical",
      "message": "string",
      "recommendation": "string"
    }}
  ],
  "duplicate_therapies": [
    {{
      "medicines": ["medicine1", "medicine2"],
      "therapeutic_class": "string",
      "recommendation": "string"
    }}
  ],
  "overall_safety_score": 0.0-1.0,
  "requires_pharmacist_review": true/false
}}

Provide thorough analysis. Return ONLY the JSON."""


def get_common_drug_interactions():
    """
    Return common drug interactions database
    This is a simplified version - in production, use a proper drug database
    """
    return {
        "nsaid_anticoagulant": {
            "drugs": ["aspirin", "ibuprofen", "diclofenac", "warfarin", "heparin"],
            "severity": "major",
            "description": "NSAIDs + anticoagulants increase bleeding risk"
        },
        "multiple_nsaids": {
            "drugs": ["aspirin", "ibuprofen", "diclofenac", "naproxen"],
            "severity": "moderate",
            "description": "Multiple NSAIDs increase GI bleeding risk"
        },
        "duplicate_antibiotics": {
            "drugs": ["amoxicillin", "azithromycin", "ciprofloxacin", "doxycycline"],
            "severity": "moderate",
            "description": "Multiple antibiotics may not be necessary"
        }
    }


def get_dosage_ranges():
    """
    Return common dosage ranges for medications
    This is simplified - in production, use a proper drug database
    """
    return {
        "aspirin": {
            "min_mg": 75,
            "max_mg": 325,
            "max_daily_mg": 325,
            "units": ["mg"]
        },
        "paracetamol": {
            "min_mg": 500,
            "max_mg": 1000,
            "max_daily_mg": 4000,
            "units": ["mg"]
        },
        "ibuprofen": {
            "min_mg": 200,
            "max_mg": 800,
            "max_daily_mg": 2400,
            "units": ["mg"]
        },
        "metformin": {
            "min_mg": 500,
            "max_mg": 1000,
            "max_daily_mg": 2550,
            "units": ["mg"]
        },
        "atorvastatin": {
            "min_mg": 10,
            "max_mg": 80,
            "max_daily_mg": 80,
            "units": ["mg"]
        }
    }
