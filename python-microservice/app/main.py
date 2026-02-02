"""
FastAPI Application - MedRemind Python Microservice
CrewAI-based prescription parser
"""

import json
import os
from datetime import datetime
from pathlib import Path

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app import __version__
from app.config import settings
from app.models import ParseRequest, ParseResponse, HealthResponse
from app.crew.prescription_crew import get_crew

# Create FastAPI app
app = FastAPI(
    title="MedRemind Python API",
    description="CrewAI-based prescription parser microservice",
    version=__version__,
    docs_url="/docs",
    redoc_url="/redoc"
)

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origins_list,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Ensure storage directory exists
STORAGE_PATH = Path(settings.storage_path)
STORAGE_PATH.mkdir(parents=True, exist_ok=True)


@app.get("/", tags=["Root"])
async def root():
    """Root endpoint"""
    return {
        "name": "MedRemind Python API",
        "version": __version__,
        "description": "CrewAI-based prescription parser",
        "docs": "/docs",
        "health": "/health"
    }


@app.get("/health", response_model=HealthResponse, tags=["Health"])
async def health_check():
    """Health check endpoint"""
    enabled_parsers = []
    if settings.openai_enabled:
        enabled_parsers.append("OpenAI")
    if settings.deepseek_enabled:
        enabled_parsers.append("DeepSeek")
    if settings.claude_enabled:
        enabled_parsers.append("Claude")
    
    return HealthResponse(
        status="healthy",
        version=__version__,
        timestamp=datetime.utcnow(),
        enabled_parsers=enabled_parsers
    )


@app.post("/api/prescription/parse", response_model=ParseResponse, tags=["Prescription"])
async def parse_prescription(request: ParseRequest):
    """
    Parse prescription using CrewAI agents
    
    Args:
        request: ParseRequest with OCR text and prescription ID
        
    Returns:
        ParseResponse with extracted prescription data
    """
    try:
        print(f"\n{'='*60}")
        print(f"?? Processing prescription: {request.prescription_id}")
        print(f"   OCR text length: {len(request.ocr_text)} characters")
        print(f"{'='*60}")
        
        # Get crew and process
        crew = get_crew()
        result = await crew.process(request.ocr_text, request.prescription_id)
        
        # Save result to file if requested
        if request.save_result and settings.enable_file_storage:
            save_path = await save_prescription_result(request.prescription_id, result)
            print(f"\n?? Saved result to: {save_path}")
        
        return result
        
    except Exception as e:
        print(f"\n? Error processing prescription: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/api/prescription/{prescription_id}", tags=["Prescription"])
async def get_prescription(prescription_id: str):
    """
    Get saved prescription result
    
    Args:
        prescription_id: Unique prescription identifier
        
    Returns:
        Saved prescription data
    """
    file_path = STORAGE_PATH / f"{prescription_id}.json"
    
    if not file_path.exists():
        raise HTTPException(status_code=404, detail="Prescription not found")
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        return JSONResponse(content=data)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error reading prescription: {str(e)}")


async def save_prescription_result(prescription_id: str, result: ParseResponse) -> str:
    """
    Save prescription result to file
    
    Args:
        prescription_id: Unique prescription identifier
        result: ParseResponse to save
        
    Returns:
        Path to saved file
    """
    file_path = STORAGE_PATH / f"{prescription_id}.json"
    
    # Convert to dict for JSON serialization
    data = result.model_dump(mode='json')
    
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    return str(file_path)


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "app.main:app",
        host=settings.host,
        port=settings.port,
        reload=True,
        log_level=settings.log_level.lower()
    )
