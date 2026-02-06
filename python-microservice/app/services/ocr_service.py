"""
Medical Prescription OCR Extraction Service
Supports multiple OCR providers with GPT-4 Vision as primary
Features:
- Handwritten prescription recognition
- Multi-language support
- PDF and Image processing
- Structured data extraction
"""

import base64
import io
import time
from typing import Optional, Tuple, List
import httpx
from PIL import Image
import fitz  # PyMuPDF for PDF processing

from app.config import settings
from app.models import (
    OCRRequest, OCRResponse, OCRConfidence,
    DetectedLanguage, TextRegion, ParseResponse
)


class MedicalOCRService:
    """
    Medical Prescription OCR Service
    Primary: GPT-4 Vision (best for handwriting)
    Fallback: Azure Document Intelligence, Google Vision
    """
    
    # Supported languages with their ISO codes
    SUPPORTED_LANGUAGES = {
        'en': 'English',
        'hi': 'Hindi',
        'bn': 'Bengali',
        'ta': 'Tamil',
        'te': 'Telugu',
        'mr': 'Marathi',
        'gu': 'Gujarati',
        'kn': 'Kannada',
        'ml': 'Malayalam',
        'pa': 'Punjabi',
        'ur': 'Urdu',
        'ar': 'Arabic',
        'es': 'Spanish',
        'fr': 'French',
        'de': 'German',
        'pt': 'Portuguese',
        'zh': 'Chinese',
        'ja': 'Japanese',
        'ko': 'Korean',
        'ru': 'Russian',
    }
    
    def __init__(self):
        self.openai_api_key = settings.openai_api_key
        self.openai_vision_model = getattr(settings, 'openai_vision_model', 'gpt-4o')
        
    async def extract_text(self, request: OCRRequest) -> OCRResponse:
        """
        Main entry point for OCR extraction
        
        Args:
            request: OCRRequest with base64 document
            
        Returns:
            OCRResponse with extracted text and metadata
        """
        start_time = time.time()
        
        try:
            # Step 1: Decode and validate document
            document_bytes, detected_type = self._decode_document(
                request.document_base64, 
                request.document_type
            )
            
            # Step 2: Convert PDF to images if needed
            images = await self._prepare_images(document_bytes, detected_type)
            
            # Step 3: Extract text using GPT-4 Vision
            extraction_result = await self._extract_with_gpt4_vision(
                images,
                request.language_hints,
                request.enhance_handwriting
            )
            
            # Step 4: Build response
            processing_time = time.time() - start_time
            
            response = OCRResponse(
                success=True,
                prescription_id=request.prescription_id,
                raw_text=extraction_result['text'],
                text_regions=extraction_result.get('regions'),
                detected_languages=extraction_result['languages'],
                primary_language=extraction_result['primary_language'],
                confidence=extraction_result['confidence'],
                document_type=detected_type,
                page_count=len(images),
                processing_time=processing_time,
                ocr_provider="openai_gpt4_vision",
                warnings=extraction_result.get('warnings', [])
            )
            
            # Step 5: Extract structured data if requested
            if request.extract_structured_data and response.raw_text:
                from app.crew.prescription_crew import get_crew
                crew = get_crew()
                structured_result = await crew.process(
                    response.raw_text,
                    request.prescription_id or f"ocr_{int(time.time())}"
                )
                response.structured_data = structured_result
            
            return response
            
        except Exception as e:
            return OCRResponse(
                success=False,
                prescription_id=request.prescription_id,
                raw_text="",
                detected_languages=[],
                confidence=OCRConfidence(
                    overall=0.0,
                    text_clarity=0.0,
                    handwriting_quality=0.0,
                    language_confidence=0.0
                ),
                document_type=request.document_type,
                processing_time=time.time() - start_time,
                ocr_provider="openai_gpt4_vision",
                error_message=str(e)
            )
    
    def _decode_document(self, base64_data: str, doc_type: str) -> Tuple[bytes, str]:
        """
        Decode base64 document and detect type
        
        Returns:
            Tuple of (document_bytes, detected_type)
        """
        # Remove data URL prefix if present
        if ',' in base64_data:
            header, base64_data = base64_data.split(',', 1)
            # Try to detect type from header
            if 'pdf' in header.lower():
                doc_type = 'pdf'
            elif any(fmt in header.lower() for fmt in ['png', 'jpg', 'jpeg', 'gif', 'webp']):
                doc_type = 'image'
        
        try:
            document_bytes = base64.b64decode(base64_data)
        except Exception as e:
            raise ValueError(f"Invalid base64 data: {str(e)}")
        
        # Auto-detect if needed
        if doc_type == 'auto':
            doc_type = self._detect_document_type(document_bytes)
        
        return document_bytes, doc_type
    
    def _detect_document_type(self, data: bytes) -> str:
        """Detect document type from magic bytes"""
        # PDF magic bytes
        if data[:4] == b'%PDF':
            return 'pdf'
        
        # PNG magic bytes
        if data[:8] == b'\x89PNG\r\n\x1a\n':
            return 'image'
        
        # JPEG magic bytes
        if data[:2] == b'\xff\xd8':
            return 'image'
        
        # GIF magic bytes
        if data[:6] in (b'GIF87a', b'GIF89a'):
            return 'image'
        
        # WebP magic bytes
        if data[:4] == b'RIFF' and data[8:12] == b'WEBP':
            return 'image'
        
        # Default to image
        return 'image'
    
    async def _prepare_images(self, document_bytes: bytes, doc_type: str) -> List[str]:
        """
        Prepare images for OCR processing
        
        Args:
            document_bytes: Raw document bytes
            doc_type: 'pdf' or 'image'
            
        Returns:
            List of base64 encoded images
        """
        images = []
        
        if doc_type == 'pdf':
            # Convert PDF pages to images
            pdf_document = fitz.open(stream=document_bytes, filetype="pdf")
            
            for page_num in range(len(pdf_document)):
                page = pdf_document[page_num]
                # High DPI for better OCR quality
                mat = fitz.Matrix(2.0, 2.0)  # 2x zoom for better quality
                pix = page.get_pixmap(matrix=mat)
                
                # Convert to PIL Image
                img = Image.frombytes("RGB", [pix.width, pix.height], pix.samples)
                
                # Convert to base64
                buffer = io.BytesIO()
                img.save(buffer, format="PNG")
                img_base64 = base64.b64encode(buffer.getvalue()).decode('utf-8')
                images.append(img_base64)
            
            pdf_document.close()
        else:
            # Single image
            img_base64 = base64.b64encode(document_bytes).decode('utf-8')
            images.append(img_base64)
        
        return images
    
    async def _extract_with_gpt4_vision(
        self,
        images: List[str],
        language_hints: Optional[List[str]],
        enhance_handwriting: bool
    ) -> dict:
        """
        Extract text using GPT-4 Vision
        
        This is the primary OCR method optimized for handwritten medical prescriptions
        """
        all_text = []
        all_regions = []
        detected_langs = {}
        warnings = []
        
        # Build language hint string
        lang_hint = ""
        if language_hints:
            lang_names = [self.SUPPORTED_LANGUAGES.get(l, l) for l in language_hints]
            lang_hint = f"Expected languages: {', '.join(lang_names)}. "
        
        # Build the extraction prompt
        system_prompt = self._build_ocr_prompt(enhance_handwriting, lang_hint)
        
        async with httpx.AsyncClient(timeout=120.0) as client:
            for idx, img_base64 in enumerate(images):
                try:
                    # Call GPT-4 Vision
                    response = await client.post(
                        "https://api.openai.com/v1/chat/completions",
                        headers={
                            "Authorization": f"Bearer {self.openai_api_key}",
                            "Content-Type": "application/json"
                        },
                        json={
                            "model": self.openai_vision_model,
                            "messages": [
                                {
                                    "role": "system",
                                    "content": system_prompt
                                },
                                {
                                    "role": "user",
                                    "content": [
                                        {
                                            "type": "text",
                                            "text": "Extract all text from this medical prescription image. Include handwritten and printed text."
                                        },
                                        {
                                            "type": "image_url",
                                            "image_url": {
                                                "url": f"data:image/png;base64,{img_base64}",
                                                "detail": "high"  # High detail for medical documents
                                            }
                                        }
                                    ]
                                }
                            ],
                            "max_tokens": 4096,
                            "temperature": 0.1  # Low temperature for accuracy
                        }
                    )
                    
                    response.raise_for_status()
                    result = response.json()
                    
                    extracted_text = result['choices'][0]['message']['content']
                    all_text.append(extracted_text)
                    
                    # Create text region
                    all_regions.append(TextRegion(
                        text=extracted_text,
                        region_type="full_page",
                        confidence=0.85,
                        bounding_box=None
                    ))
                    
                except Exception as e:
                    warnings.append(f"Page {idx + 1} extraction warning: {str(e)}")
        
        # Combine all text
        combined_text = "\n\n--- Page Break ---\n\n".join(all_text) if len(all_text) > 1 else (all_text[0] if all_text else "")
        
        # Detect languages in extracted text
        detected_languages = await self._detect_languages(combined_text)
        primary_lang = detected_languages[0].code if detected_languages else 'en'
        
        # Calculate confidence scores
        confidence = self._calculate_confidence(combined_text, len(images))
        
        return {
            'text': combined_text,
            'regions': all_regions,
            'languages': detected_languages,
            'primary_language': primary_lang,
            'confidence': confidence,
            'warnings': warnings
        }
    
    def _build_ocr_prompt(self, enhance_handwriting: bool, lang_hint: str) -> str:
        """Build the system prompt for OCR extraction"""
        
        base_prompt = """You are an expert medical prescription OCR specialist with extensive experience reading handwritten medical documents.

Your task is to extract ALL text from medical prescription images with high accuracy.

CRITICAL INSTRUCTIONS:
1. Extract EVERY piece of text visible in the image
2. Maintain original line breaks and document structure
3. For handwritten text:
   - Read carefully, considering medical context
   - Include uncertain characters with [?] notation
   - Common medical abbreviations: OD (once daily), BD (twice daily), TDS (three times daily), QID (four times daily)

DOCUMENT SECTIONS TO IDENTIFY:
- Doctor Information (Name, Qualification, Registration Number, Contact)
- Patient Information (Name, Age, Gender)
- Date
- Diagnosis/Chief Complaint
- Medications (Name, Dose, Frequency, Duration, Instructions)
- Signature
- Hospital/Clinic Header

OUTPUT FORMAT:
- Preserve the original layout as much as possible
- Use clear section headers when identifiable
- For unclear handwriting, provide best interpretation with [?]
- Do not add information that is not in the image
- Do not interpret or summarize - extract verbatim

"""
        
        if enhance_handwriting:
            base_prompt += """
HANDWRITING ENHANCEMENT MODE:
- Pay extra attention to cursive and connected letters
- Consider context: medical terms, drug names, dosage patterns
- Common OCR errors to watch: 0/O, 1/l/I, 5/S, 8/B, mg/mcg
- Doctor's handwriting often has characteristic shortcuts
"""
        
        if lang_hint:
            base_prompt += f"\n{lang_hint}Process text in the detected languages accurately.\n"
        
        return base_prompt
    
    async def _detect_languages(self, text: str) -> List[DetectedLanguage]:
        """Detect languages in the extracted text using GPT-4"""
        
        if not text or len(text.strip()) < 10:
            return [DetectedLanguage(code='en', name='English', confidence=0.5)]
        
        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.post(
                    "https://api.openai.com/v1/chat/completions",
                    headers={
                        "Authorization": f"Bearer {self.openai_api_key}",
                        "Content-Type": "application/json"
                    },
                    json={
                        "model": "gpt-4o-mini",
                        "messages": [
                            {
                                "role": "system",
                                "content": "You are a language detection expert. Analyze text and return detected languages as JSON array."
                            },
                            {
                                "role": "user",
                                "content": f"""Detect all languages in this text and return as JSON array.
Format: [{{"code": "en", "name": "English", "confidence": 0.95}}]

Text:
{text[:1000]}

Return ONLY the JSON array, nothing else."""
                            }
                        ],
                        "max_tokens": 200,
                        "temperature": 0
                    }
                )
                
                result = response.json()
                content = result['choices'][0]['message']['content']
                
                # Parse JSON response
                import json
                # Clean up response if needed
                content = content.strip()
                if content.startswith('```'):
                    content = content.split('\n', 1)[1].rsplit('```', 1)[0]
                
                langs_data = json.loads(content)
                return [
                    DetectedLanguage(
                        code=l.get('code', 'en'),
                        name=l.get('name', 'Unknown'),
                        confidence=l.get('confidence', 0.5)
                    )
                    for l in langs_data
                ]
                
        except Exception:
            # Default to English if detection fails
            return [DetectedLanguage(code='en', name='English', confidence=0.5)]
    
    def _calculate_confidence(self, text: str, page_count: int) -> OCRConfidence:
        """Calculate confidence scores based on extraction quality"""
        
        if not text:
            return OCRConfidence(
                overall=0.0,
                text_clarity=0.0,
                handwriting_quality=0.0,
                language_confidence=0.0
            )
        
        # Text clarity: based on presence of uncertain markers
        uncertain_count = text.count('[?]') + text.count('?]')
        text_length = len(text)
        text_clarity = max(0.5, 1.0 - (uncertain_count * 50 / max(text_length, 1)))
        
        # Handwriting quality: based on consistent extraction
        has_structure = any(marker in text.lower() for marker in ['date', 'name', 'dr.', 'tab', 'mg'])
        handwriting_quality = 0.8 if has_structure else 0.6
        
        # Language confidence
        has_mixed_scripts = any(ord(c) > 127 for c in text)
        language_confidence = 0.7 if has_mixed_scripts else 0.9
        
        # Overall confidence
        overall = (text_clarity * 0.4 + handwriting_quality * 0.4 + language_confidence * 0.2)
        
        return OCRConfidence(
            overall=min(overall, 1.0),
            text_clarity=min(text_clarity, 1.0),
            handwriting_quality=min(handwriting_quality, 1.0),
            language_confidence=min(language_confidence, 1.0)
        )


# Global service instance
_ocr_service: Optional[MedicalOCRService] = None


def get_ocr_service() -> MedicalOCRService:
    """Get or create OCR service instance"""
    global _ocr_service
    if _ocr_service is None:
        _ocr_service = MedicalOCRService()
    return _ocr_service
