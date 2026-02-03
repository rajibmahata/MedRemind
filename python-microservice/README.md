# MedRemind Python Microservice - CrewAI Prescription Parser

FastAPI microservice using CrewAI for prescription data extraction.

## ?? Features

- **CrewAI Multi-Agent System** - Sequential agent pipeline
- **FastAPI REST API** - Easy integration with .NET
- **Configuration Sync** - Uses .NET project's LLM configs
- **File Storage** - Saves parsed prescriptions for validation
- **Health Checks** - Monitoring endpoints- **Agent Tracing** - LangSmith integration for tracking agent execution
## ?? Requirements

- Python 3.11+
- uv (for package management)
- OpenAI API Key
- DeepSeek API Key (optional)
- Claude API Key (optional)

## ?? Quick Start

### 1. Install uv (if not installed)

```bash
# Windows (PowerShell)
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### 2. Setup Virtual Environment

```bash
cd python-microservice

# Create venv with uv
uv venv

# Activate venv
# Windows
.venv\Scripts\activate

# macOS/Linux
source .venv/bin/activate
```

### 3. Install Dependencies

```bash
# Install all dependencies with uv
uv pip install -r requirements.txt
```

### 4. Configure Environment

Create `.env` file:
```env
OPENAI_API_KEY=your_openai_key_here
OPENAI_MODEL=gpt-4o-mini
DEEPSEEK_API_KEY=your_deepseek_key_here
DEEPSEEK_MODEL=deepseek-chat
CLAUDE_API_KEY=your_claude_key_here
CLAUDE_MODEL=claude-3-5-sonnet-20241022

# Optional: Enable specific parsers
OPENAI_ENABLED=true
DEEPSEEK_ENABLED=false
CLAUDE_ENABLED=false

# Server config
HOST=0.0.0.0
PORT=8000

# Tracing (Optional - for monitoring agent execution)
# Sign up at https://smith.langchain.com/ to get an API key
LANGCHAIN_TRACING_V2=true
LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
LANGCHAIN_API_KEY=your_langsmith_api_key_here
LANGCHAIN_PROJECT=medremind-prescription-parser
```

### 5. Run the Server

```bash
# Development mode (auto-reload)
uvicorn app.main:app --reload --port 8000

# Production mode
uvicorn app.main:app --host 0.0.0.0 --port 8000 --workers 4
```

## ?? API Endpoints

### Health Check
```http
GET http://localhost:8000/health
```

### Parse Prescription
```http
POST http://localhost:8000/api/prescription/parse
Content-Type: application/json

{
  "ocr_text": "Date: 28/1/26\nDr. Smith\nTab Aspirin 75mg...",
  "prescription_id": "prescription_1_20260128_193022",
  "save_result": true
}
```

### Get Parsed Prescription
```http
GET http://localhost:8000/api/prescription/{prescription_id}
```

## ?? CrewAI Agents

### Agent 1: OCR Normalizer
- **Role:** Clean and normalize OCR text
- **Goal:** Remove noise, fix OCR errors
- **Output:** Normalized text

### Agent 2: Data Extractor
- **Role:** Extract structured data
- **Goal:** Parse medications, patient, doctor info
- **Output:** Structured JSON

### Agent 3: Safety Validator
- **Role:** Validate medical safety
- **Goal:** Check for conflicts, safety issues
- **Output:** Validated prescription with warnings
### Agent 4: Medicine Validator
- **Role:** Clinical Pharmacist & Medicine Validator
- **Goal:** Validate medicine names, dosages, and drug interactions
- **Output:** Drug interactions, safety warnings, duplicate therapy detection
- **Features:**
  - Verifies medicine names and corrects OCR errors
  - Validates dosages within therapeutic ranges
  - Identifies potential drug-drug interactions
  - Flags duplicate therapies (same drug class)
  - Provides safety warnings with severity levels
## ?? File Structure

```
python-microservice/
??? app/
?   ??? __init__.py
?   ??? main.py                 # FastAPI app
?   ??? config.py               # Configuration loader
?   ??? models.py               # Pydantic models
?   ??? agents/
?   ?   ??? __init__.py
?   ?   ??? normalizer.py       # Agent 1
?   ?   ??? extractor.py        # Agent 2
?   ?   ??? validator.py        # Agent 3
?   ?   ??? medicine_validator.py # Agent 4
?   ??? crew/
?       ??? __init__.py
?       ??? prescription_crew.py # CrewAI orchestrator
??? storage/
?   ??? parsed_prescriptions/   # Saved results
??? tests/
?   ??? test_api.py
??? .env
??? .gitignore
??? requirements.txt
??? pyproject.toml
??? README.md
```

## ?? Integration with .NET

### From .NET (C#)

```csharp
var client = new HttpClient();
var request = new
{
    ocr_text = ocrText,
    prescription_id = prescriptionFileName,
    save_result = true
};

var response = await client.PostAsJsonAsync(
    "http://localhost:8000/api/prescription/parse", 
    request);

var result = await response.Content.ReadFromJsonAsync<PrescriptionResult>();
```

## ?? Response Format

```json
{
  "success": true,
  "prescription_id": "prescription_1_20260128_193022",
  "patient": {
    "name": "Mr. Rajib Monata",
    "age": 34,
    "gender": "M"
  },
  "doctor": {
    "name": "Dr. Shrinivas Narayan",
    "specialization": "Urology",
    "registration_number": "70128 WBMC"
  },
  "prescription_date": "2026-01-28",
  "medications": [
    {
      "name": "Fabulas",
      "dosage": "240",
      "unit": "mg",
      "frequency": "Once daily",
      "frequency_count": 1,
      "duration_days": 21,
      "instructions": "For 3 weeks",
      "purpose": "Treatment of urinary tract conditions",
      "side_effects": ["Nausea", "Headache", "Dizziness"],
      "confidence_score": 0.9
    }
  ],
  "medicine_validation": {
    "drug_interactions": [
      {
        "medicines": ["Aspirin", "Warfarin"],
        "severity": "major",
        "description": "NSAIDs + anticoagulants increase bleeding risk",
        "recommendation": "Monitor INR closely"
      }
    ],
    "safety_warnings": [
      {
        "medicine": "Aspirin",
        "type": "dosage",
        "severity": "medium",
        "message": "Dosage is at upper limit",
        "recommendation": "Consider lower dose if bleeding risk"
      }
    ],
    "duplicate_therapies": [],
    "overall_safety_score": 0.85,
    "requires_pharmacist_review": false
  },
  "warnings": [],
  "processing_time": 3.45,
  "crew_summary": "Crew executed successfully..."
}
```

## 🔍 Agent Tracing & Monitoring

This service supports **LangSmith tracing** to monitor agent execution, track LLM calls, and debug issues.

### Setup LangSmith Tracing

1. **Sign up for LangSmith** at [smith.langchain.com](https://smith.langchain.com/)
2. **Get your API key** from the settings page
3. **Update `.env` file**:
   ```env
   LANGCHAIN_TRACING_V2=true
   LANGCHAIN_API_KEY=your_langsmith_api_key_here
   LANGCHAIN_PROJECT=medremind-prescription-parser
   ```

### What Gets Tracked

- **Agent Execution** - Each agent's input/output
- **LLM Calls** - Prompts and responses
- **Task Performance** - Execution time for each step
- **Errors & Warnings** - Failed operations and retries
- **Token Usage** - Cost tracking per request

### Viewing Traces

1. Visit [smith.langchain.com](https://smith.langchain.com/)
2. Navigate to your project
3. View detailed traces for each prescription parsing request
4. Analyze agent performance and identify bottlenecks

### Disable Tracing

Set in `.env`:
```env
LANGCHAIN_TRACING_V2=false
```

**📘 For detailed tracing setup and usage, see [TRACING_GUIDE.md](TRACING_GUIDE.md)**

## ?? Testing

```bash
# Run tests
pytest tests/ -v

# Test with curl
curl -X POST http://localhost:8000/api/prescription/parse \
  -H "Content-Type: application/json" \
  -d '{"ocr_text": "Date: 28/1/26\nTab Aspirin 75mg", "prescription_id": "test_001"}'
```

## ?? Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| OPENAI_API_KEY | OpenAI API key | Required |
| OPENAI_MODEL | Model to use | gpt-4o-mini |
| DEEPSEEK_API_KEY | DeepSeek API key | Optional |
| CLAUDE_API_KEY | Claude API key | Optional |
| HOST | Server host | 0.0.0.0 |
| PORT | Server port | 8000 |
| LOG_LEVEL | Logging level | INFO |
| LANGCHAIN_TRACING_V2 | Enable LangSmith tracing | false |
| LANGCHAIN_API_KEY | LangSmith API key | Optional |
| LANGCHAIN_PROJECT | LangSmith project name | medremind-prescription-parser |

## ?? Security

- API keys stored in `.env` (not in git)
- CORS configured for .NET origin
- Input validation with Pydantic
- Rate limiting (optional)

## ?? Deployment

### Docker

```bash
docker build -t medremind-python-api .
docker run -p 8000:8000 --env-file .env medremind-python-api
```

### systemd Service

```ini
[Unit]
Description=MedRemind Python API
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/medremind-python
Environment="PATH=/opt/medremind-python/.venv/bin"
ExecStart=/opt/medremind-python/.venv/bin/uvicorn app.main:app --host 0.0.0.0 --port 8000
Restart=always

[Install]
WantedBy=multi-user.target
```

## ?? Next Steps

1. Configure your API keys in `.env`
2. Run the server: `uvicorn app.main:app --reload`
3. Test with Postman or curl
4. Integrate with .NET project
5. Deploy to production

## ?? Documentation

- [CrewAI Documentation](https://docs.crewai.com/)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Pydantic Documentation](https://docs.pydantic.dev/)

## ?? Troubleshooting

### Issue: Import errors
```bash
# Reinstall dependencies
uv pip install -r requirements.txt --force-reinstall
```

### Issue: Port already in use
```bash
# Change port in .env or command
uvicorn app.main:app --port 8001
```

### Issue: CrewAI agent errors
- Check API keys in `.env`
- Verify model names match provider specs
- Check logs in `storage/logs/`

### run the application 
``` bash

uv run uvicorn app.main:app --reload --port 8000

```
### Swagger Doc
- API Documentation: http://localhost:8000/docs
- Health Check: http://localhost:8000/health