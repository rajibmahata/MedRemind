"""
Services module for MedRemind Python Microservice
"""

from app.services.ocr_service import MedicalOCRService, get_ocr_service

__all__ = ['MedicalOCRService', 'get_ocr_service']
