# ?? Python CrewAI Microservice - Quick Start

5-minute setup guide for the Python microservice.

---

## ? Quick Setup (Windows)

```powershell
# 1. Navigate to folder
cd F:\rajibmahata\MedRemind\python-microservice

# 2. Run setup
.\setup.bat

# 3. Edit .env (add your OpenAI key)
notepad .env

# 4. Activate venv
.venv\Scripts\activate

# 5. Run server
uvicorn app.main:app --reload
```

---

## ? Quick Setup (Unix/macOS)

```bash
# 1. Navigate to folder
cd /path/to/MedRemind/python-microservice

# 2. Run setup
chmod +x setup.sh
./setup.sh

# 3. Edit .env (add your OpenAI key)
nano .env

# 4. Activate venv
source .venv/bin/activate

# 5. Run server
uvicorn app.main:app --reload
```

---

## ?? Minimum .env Configuration

```env
OPENAI_API_KEY=sk-your-actual-key-here
OPENAI_MODEL=gpt-4o-mini
OPENAI_ENABLED=true

HOST=0.0.0.0
PORT=8000
```

---

## ? Test Python Service

```bash
# 1. Check health
curl http://localhost:8000/health

# 2. Test parsing
curl -X POST http://localhost:8000/api/prescription/parse \
  -H "Content-Type: application/json" \
  -d '{"ocr_text":"Date: 28/1/26\nTab Aspirin 75mg","prescription_id":"test_001","save_result":true}'
```

---

## ?? Integrate with .NET

### Step 1: Register Client in Program.cs

```csharp
// In Program.cs
builder.Services.AddHttpClient<PythonCrewAIClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");
    client.Timeout = TimeSpan.FromMinutes(5);
});
```

### Step 2: Use in Service

```csharp
public class MyService
{
    private readonly PythonCrewAIClient _pythonClient;
    
    public MyService(PythonCrewAIClient pythonClient)
    {
        _pythonClient = pythonClient;
    }
    
    public async Task<PrescriptionResult> ProcessAsync(string ocrText, string prescriptionId)
    {
        // Call Python service
        var result = await _pythonClient.ParsePrescriptionAsync(
            ocrText, 
            prescriptionId,
            saveResult: true);
        
        if (result?.Success == true)
        {
            // Success! Use result
            Console.WriteLine($"Date: {result.PrescriptionDate}");
            Console.WriteLine($"Meds: {result.Medications.Count}");
            return MapToResult(result);
        }
        
        return null;
    }
}
```

---

## ?? Verify It's Working

### Check 1: Health Endpoint
```
http://localhost:8000/health
Should return: {"status":"healthy"}
```

### Check 2: API Documentation
```
http://localhost:8000/docs
Should show: Swagger UI
```

### Check 3: Parse Test
```powershell
# PowerShell
$body = @{
    ocr_text = "Date: 28/1/26`nDr. Smith`nTab Aspirin 75mg"
    prescription_id = "test_001"
    save_result = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8000/api/prescription/parse" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### Check 4: Saved File
```
Check: python-microservice/storage/parsed_prescriptions/test_001.json
Should exist with parsed data
```

---

## ?? Common Issues

### Issue 1: Port 8000 in use
```bash
# Change port
uvicorn app.main:app --reload --port 8001

# Update .NET client
client.BaseAddress = new Uri("http://localhost:8001");
```

### Issue 2: Module not found
```bash
# Reinstall dependencies
uv pip install -r requirements.txt --force-reinstall
```

### Issue 3: OpenAI API error
```bash
# Check your API key in .env
cat .env | grep OPENAI_API_KEY

# Test key directly
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_KEY"
```

### Issue 4: CrewAI import error
```bash
# Install specific version
uv pip install crewai==0.76.7 crewai-tools==0.12.1
```

---

## ?? File Locations

```
python-microservice/
??? .env                           ? Your API keys here
??? storage/
?   ??? parsed_prescriptions/      ? Saved results here
??? app/
    ??? main.py                    ? FastAPI app
```

---

## ?? Quick Commands

```bash
# Start server
uvicorn app.main:app --reload

# Start on different port
uvicorn app.main:app --reload --port 8001

# Production mode (4 workers)
uvicorn app.main:app --host 0.0.0.0 --port 8000 --workers 4

# Run tests
pytest tests/ -v

# Check dependencies
uv pip list

# Update dependencies
uv pip install -r requirements.txt --upgrade
```

---

## ? Checklist

Before integrating with .NET:

- [ ] Python 3.11+ installed
- [ ] uv installed
- [ ] Virtual environment created (`.venv/`)
- [ ] Dependencies installed
- [ ] `.env` configured with API key
- [ ] Server running on port 8000
- [ ] Health check passes
- [ ] Test parsing works
- [ ] `.NET client registered in Program.cs`
- [ ] Test from .NET succeeds

---

## ?? You're Ready!

```
? Python service: http://localhost:8000
? API docs: http://localhost:8000/docs
? .NET integration: Use PythonCrewAIClient
? Saved prescriptions: storage/parsed_prescriptions/
```

**The microservice is running and ready for .NET integration! ??**
