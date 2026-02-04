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
        goal="Validate medicine names, dosages, and identify potential drug interactions with age-specific considerations",
        backstory="""You are a licensed clinical pharmacist with extensive knowledge of:
        - Generic and brand names of medicines
        - Age-specific dosage ranges (pediatric, adult, geriatric)
        - Drug-drug interactions and contraindications
        - Common prescription errors and red flags
        - Pediatric dosing calculations (mg/kg body weight)
        - Geriatric dosing adjustments and special precautions
        
        Your expertise includes:
        1. Verifying medicine names (correcting OCR errors)
        2. Validating dosages are appropriate for patient age
        3. Identifying age-inappropriate medications
        4. Calculating pediatric doses and checking for safety
        5. Flagging medications requiring dose reduction in elderly
        6. Identifying potential drug interactions
        7. Recognizing duplicate therapies (same drug class)
        
        You understand that:
        - Pediatric patients (<18 years) often require weight-based dosing
        - Geriatric patients (>65 years) may need reduced doses
        - Some medications are contraindicated in specific age groups
        - Age affects drug metabolism and clearance
        
        You provide detailed age-specific warnings but don't reject prescriptions outright.""",
        llm=llm,
        verbose=True,
        allow_delegation=False
    )


MEDICINE_VALIDATION_PROMPT = """Validate the following medications for accuracy, safety, and interactions with SPECIAL EMPHASIS on age-appropriate dosing.

VALIDATION CHECKS:
1. **Medicine Name Validation**
   - Verify each medicine name is correct (check for OCR errors)
   - Suggest corrections for misspelled medicine names
   - Identify if it's generic or brand name

2. **Age-Based Dosage Validation** (CRITICAL)
   - For PEDIATRIC patients (<18 years):
     * Check if dosage is appropriate for age
     * Verify dose is within pediatric range
     * Flag if adult doses are prescribed to children
     * Check for contraindicated medications in children
   - For ADULT patients (18-64 years):
     * Check if dosage is within standard therapeutic range
     * Verify dose is appropriate for general adult population
   - For GERIATRIC patients (≥65 years):
     * Check if dose reduction is needed
     * Flag medications on Beers Criteria (inappropriate for elderly)
     * Verify dosage accounts for reduced renal/hepatic function
     * Flag high-risk medications in elderly (benzodiazepines, anticholinergics)
   - General:
     * Flag if dosage is unusually high or low for the patient's age
     * Verify units are appropriate (mg, mcg, IU, etc.)
     * Calculate if total daily dose is safe for patient age

3. **Drug Interactions**
   - Identify potential drug-drug interactions
   - Flag contraindicated combinations
   - Note if medicines from same class (duplicate therapy)

4. **Safety Concerns**
   - Extremely high doses for patient age
   - Medications that require monitoring
   - Age-inappropriate medications
   - Frequency exceeding maximum daily dose for age group
   - Medications requiring dose adjustment based on age

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
      "age_appropriate": true/false,
      "age_category": "pediatric|adult|geriatric",
      "age_specific_warning": "string or null - warnings specific to patient age",
      "recommended_dose_for_age": "string or null - recommended dose range for this age",
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
    Return age-specific dosage ranges for common medications
    This is simplified - in production, use a proper drug database with detailed age/weight ranges
    """
    return {
        "aspirin": {
            "pediatric": {
                "note": "Generally avoided in children <12 due to Reye's syndrome risk",
                "contraindicated": True,
                "min_age": 12
            },
            "adult": {
                "min_mg": 75,
                "max_mg": 325,
                "max_daily_mg": 325,
                "units": ["mg"]
            },
            "geriatric": {
                "min_mg": 75,
                "max_mg": 325,
                "max_daily_mg": 325,
                "note": "Monitor for GI bleeding, use with caution",
                "units": ["mg"]
            }
        },
        "paracetamol": {
            "pediatric": {
                "dose_per_kg": {"min": 10, "max": 15},
                "max_daily_mg_per_kg": 75,
                "absolute_max_daily": 2000,
                "note": "Dose based on weight: 10-15mg/kg per dose",
                "units": ["mg"]
            },
            "adult": {
                "min_mg": 500,
                "max_mg": 1000,
                "max_daily_mg": 4000,
                "units": ["mg"]
            },
            "geriatric": {
                "min_mg": 500,
                "max_mg": 1000,
                "max_daily_mg": 3000,
                "note": "Reduce max daily dose to 3000mg due to decreased hepatic function",
                "units": ["mg"]
            }
        },
        "ibuprofen": {
            "pediatric": {
                "dose_per_kg": {"min": 5, "max": 10},
                "max_daily_mg_per_kg": 40,
                "min_age": 6,
                "note": "Not recommended under 6 months. Dose: 5-10mg/kg per dose",
                "units": ["mg"]
            },
            "adult": {
                "min_mg": 200,
                "max_mg": 800,
                "max_daily_mg": 2400,
                "units": ["mg"]
            },
            "geriatric": {
                "min_mg": 200,
                "max_mg": 400,
                "max_daily_mg": 1200,
                "note": "Use lowest effective dose. High risk of GI bleeding and renal issues in elderly",
                "units": ["mg"]
            }
        },
        "metformin": {
            "pediatric": {
                "min_age": 10,
                "min_mg": 500,
                "max_mg": 1000,
                "max_daily_mg": 2000,
                "note": "Approved for children ≥10 years with type 2 diabetes",
                "units": ["mg"]
            },
            "adult": {
                "min_mg": 500,
                "max_mg": 1000,
                "max_daily_mg": 2550,
                "units": ["mg"]
            },
            "geriatric": {
                "min_mg": 500,
                "max_mg": 1000,
                "max_daily_mg": 2000,
                "note": "Monitor renal function. Contraindicated if eGFR <30. Reduce dose if eGFR 30-45",
                "units": ["mg"]
            }
        },
        "atorvastatin": {
            "pediatric": {
                "min_age": 10,
                "min_mg": 10,
                "max_mg": 20,
                "max_daily_mg": 20,
                "note": "Limited use in children, only for familial hypercholesterolemia",
                "units": ["mg"]
            },
            "adult": {
                "min_mg": 10,
                "max_mg": 80,
                "max_daily_mg": 80,
                "units": ["mg"]
            },
            "geriatric": {
                "min_mg": 10,
                "max_mg": 40,
                "max_daily_mg": 40,
                "note": "Start with lower doses. Increased risk of myopathy in elderly",
                "units": ["mg"]
            }
        }
    }


def get_age_inappropriate_medications():
    """
    Return medications that are inappropriate or require special caution in specific age groups
    Based on pediatric guidelines and Beers Criteria for elderly
    """
    return {
        "beers_criteria_elderly": [
            "diphenhydramine",
            "diazepam",
            "alprazolam",
            "promethazine",
            "amitriptyline",
            "cyclobenzaprine"
        ],
        "pediatric_contraindicated": [
            "aspirin",  # <12 years - Reye's syndrome risk
            "tetracycline",  # <8 years - teeth staining
            "ciprofloxacin",  # <18 years - cartilage damage
            "codeine"  # <12 years - respiratory depression
        ],
        "geriatric_high_risk": [
            "nsaids",  # GI bleeding, renal issues
            "anticholinergics",  # Cognitive impairment
            "benzodiazepines",  # Falls, confusion
            "tricyclic_antidepressants"  # Anticholinergic effects
        ]
    }
