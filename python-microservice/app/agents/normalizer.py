"""
Agent 1: Medical OCR Normalizer
Role: Clean and normalize OCR text from prescriptions
Goal: Remove noise, standardize medical abbreviations, fix OCR errors
"""

import re
from crewai import Agent


def create_normalizer_agent(llm) -> Agent:
    """Create the OCR normalizer agent"""
    return Agent(
        role="Medical OCR Normalizer",
        goal="Clean and normalize OCR text from prescriptions",
        backstory="""You are an expert in medical abbreviations and OCR noise patterns.
        Your specialty is cleaning and normalizing text extracted from prescription images.
        You understand common OCR errors and can fix them systematically.""",
        llm=llm,
        verbose=True,
        allow_delegation=False
    )


def normalize_ocr_text(ocr_text: str) -> str:
    """
    Pre-process OCR text before sending to agent
    This reduces token usage by doing simple cleaning first
    """
    if not ocr_text:
        return ""
    
    # Remove [Handwritten: ...] tags
    text = re.sub(r'\[Handwritten:\s*([^\]]+)\]', r'\1', ocr_text)
    
    # Normalize line breaks
    text = text.replace('\r\n', '\n').replace('\r', '\n')
    
    # Remove excessive whitespace
    text = re.sub(r'[ \t]+', ' ', text)
    text = re.sub(r'\n\s*\n\s*\n+', '\n\n', text)
    
    # Fix common OCR errors
    replacements = {
        r'\b0D\b': 'OD',
        r'\bT0S\b': 'TDS',
        r'\bTD5\b': 'TDS',
        r'\bQD5\b': 'QDS',
    }
    
    for pattern, replacement in replacements.items():
        text = re.sub(pattern, replacement, text, flags=re.IGNORECASE)
    
    # Standardize date format
    text = re.sub(r'Date\s*[:\.]?\s*', 'Date: ', text, flags=re.IGNORECASE)
    
    # Remove duplicate consecutive lines
    lines = [line.strip() for line in text.split('\n') if line.strip()]
    deduplicated = []
    last_line = None
    for line in lines:
        if line != last_line:
            deduplicated.append(line)
            last_line = line
    
    return '\n'.join(deduplicated)
