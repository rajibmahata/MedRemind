"""
Agent 2: Prescription Data Extractor
Role: Extract structured medical data in strict JSON format
Goal: Parse normalized text into structured prescription data
"""

from crewai import Agent


def create_extractor_agent(llm) -> Agent:
    """Create the data extractor agent"""
    return Agent(
        role="Prescription Data Extractor",
        goal="Extract structured medical data in strict JSON format",
        backstory="""You are a clinical data structuring specialist with expertise in medical records.
        Your job is to parse prescription text and extract structured information about:
        - Patient details (name, age, gender)
        - Doctor details (name, specialization, registration number)
        - Prescription date (always convert to YYYY-MM-DD format)
        - Medications (name, dosage, frequency, duration, instructions)
        
        You are extremely careful about date formats, especially single-digit months like '28/1/26'.
        You ALWAYS extract dates when they are present in the text.""",
        llm=llm,
        verbose=True,
        allow_delegation=False
    )


EXTRACTION_PROMPT = """Extract structured prescription data from the following normalized text.

CRITICAL RULES FOR DATE EXTRACTION:
1. Look for "Date:" followed by date patterns
2. Handle single-digit months: "28/1/26" ? "2026-01-28"
3. Common formats:
   - D/M/YY (e.g., "28/1/26" ? "2026-01-28")
   - DD/MM/YYYY (e.g., "28/01/2025" ? "2025-01-28")
   - DD-MM-YYYY (e.g., "28-01-2025" ? "2025-01-28")
4. If year is 2 digits, assume 20XX
5. ALWAYS extract date if present - DO NOT return null!

CRITICAL RULES FOR MEDICATION FREQUENCY:
1. '0 0 0' ? "Three times daily", frequency_count: 3
2. '0 0' ? "Twice daily", frequency_count: 2
3. '0' or '01' ? "Once daily", frequency_count: 1
4. 'OD' ? "Once daily", frequency_count: 1
5. 'BD' ? "Twice daily", frequency_count: 2
6. 'TDS' or 'TID' ? "Three times daily", frequency_count: 3

DURATION CONVERSION:
- "1 week" or "1 wout" ? duration_days: 7
- "2 weeks" ? duration_days: 14
- "3 weeks" or "3 wout" ? duration_days: 21
- "1 month" ? duration_days: 30

Return ONLY valid JSON in this exact format:
{{
  "patient": {{
    "name": "string or null",
    "age": number or null,
    "gender": "string or null"
  }},
  "doctor": {{
    "name": "string or null",
    "specialization": "string or null",
    "registration_number": "string or null"
  }},
  "prescription_date": "YYYY-MM-DD or null",
  "medications": [
    {{
      "name": "string (required)",
      "dosage": "string",
      "unit": "string",
      "frequency": "string",
      "frequency_count": number,
      "duration": "string",
      "duration_days": number,
      "instructions": "string",
      "confidence_score": 0.0-1.0
    }}
  ]
}}

Normalized Text:
{normalized_text}

Extract the data and return ONLY the JSON."""
