"""
Agent 3: Medical Safety Validator
Role: Validate medical correctness and safety
Goal: Ensure prescription data is medically sound and safe
"""

from crewai import Agent


def create_validator_agent(llm) -> Agent:
    """Create the safety validator agent"""
    return Agent(
        role="Medical Safety Validator",
        goal="Validate medical correctness and safety",
        backstory="""You are a pharmacology and clinical safety expert with years of experience.
        Your job is to validate extracted prescription data for:
        - Completeness (all required fields present)
        - Medical safety (no duplicate medications, reasonable dosages)
        - Data consistency (valid dates, reasonable frequencies)
        - Dosing appropriateness (dosage ranges make sense)
        
        You identify potential issues and provide warnings but don't reject prescriptions.""",
        llm=llm,
        verbose=True,
        allow_delegation=False
    )


VALIDATION_PROMPT = """Validate the following extracted prescription data for medical safety and completeness.

Check for:
1. Required fields (patient name, medications)
2. Duplicate medications
3. Excessive frequencies (>6 times daily)
4. Unreasonable durations (>365 days)
5. Missing prescription date
6. Low confidence scores (<0.7)

Extracted Data:
{extracted_data}

Return a JSON object with:
{{
  "is_valid": true/false,
  "warnings": ["list", "of", "warnings"],
  "errors": ["list", "of", "errors"]
}}

If there are no errors (only warnings), set is_valid to true.
Return ONLY the JSON."""
