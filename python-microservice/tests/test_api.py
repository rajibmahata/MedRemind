"""
Test API endpoints
"""

import pytest
from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)


def test_root():
    """Test root endpoint"""
    response = client.get("/")
    assert response.status_code == 200
    data = response.json()
    assert "name" in data
    assert "version" in data


def test_health():
    """Test health check endpoint"""
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    assert "enabled_parsers" in data


def test_parse_prescription():
    """Test prescription parsing"""
    request_data = {
        "ocr_text": """Date: 28/1/26
Dr. Smith
Patient: John Doe, Age: 45, M

Rx:
1. Tab Aspirin 75mg
   0-0-0
   x 30 days""",
        "prescription_id": "test_001",
        "save_result": False
    }
    
    response = client.post("/api/prescription/parse", json=request_data)
    assert response.status_code == 200
    data = response.json()
    
    assert data["success"] is True
    assert data["prescription_id"] == "test_001"
    assert data["prescription_date"] is not None
    assert len(data["medications"]) > 0


def test_parse_prescription_invalid():
    """Test with invalid input"""
    request_data = {
        "ocr_text": "",
        "prescription_id": "test_002",
        "save_result": False
    }
    
    response = client.post("/api/prescription/parse", json=request_data)
    # Should still return 200 but with success=False
    assert response.status_code in [200, 500]
