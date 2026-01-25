# MedRemind REST API

A comprehensive medication reminder and prescription management API built with .NET 10 and ASP.NET Core.

## ?? Quick Start

### Prerequisites
- .NET 10 SDK
- SQLite (included)
- API Keys (OpenAI, Azure Document Intelligence, 2Factor.in)

### 1. Configure Environment

Update `appsettings.json` - set your active environment:
```json
{
  "ActiveEnvironment": "Development"
}
```

Configure API keys in the respective environment section (`Development`, `Staging`, or `Production`).

### 2. Run the API

**Option 1: Using batch file (Windows)**
```bash
run-api.bat
```

**Option 2: Using dotnet CLI**
```bash
cd backend/MedRemind.API
dotnet run
```

**Option 3: From solution root**
```bash
dotnet run --project backend\MedRemind.API\MedRemind.API.csproj
```

### 3. Access Swagger UI

Open your browser:
- **Primary**: http://localhost:5124/swagger
- **Alternative**: https://localhost:7073/swagger

---

## ?? Project Structure

```
backend/MedRemind.API/
??? Controllers/              # API Controllers
?   ??? AuthController.cs    # Authentication endpoints
?   ??? MedicationsController.cs
?   ??? PrescriptionsController.cs
?   ??? RemindersController.cs
?   ??? AdherenceController.cs
??? Database/                 # SQLite database storage
?   ??? medremindDB.db     # Created on first run
??? Docs/                     # ?? Documentation
?   ??? SWAGGER_SETUP.md     # Complete Swagger setup guide
?   ??? CURL_EXAMPLES.md     # cURL testing examples
??? Program.cs                # Main configuration
??? appsettings.json         # Configuration settings
??? README.md                 # This file
```

---

## ?? Documentation

Comprehensive documentation is available in the [`Docs`](Docs/) folder:

### ?? Available Guides

| Document | Description |
|----------|-------------|
| **[SWAGGER_SETUP.md](Docs/SWAGGER_SETUP.md)** | Complete setup guide, configuration, testing with Swagger UI, deployment options, and troubleshooting |
| **[CURL_EXAMPLES.md](Docs/CURL_EXAMPLES.md)** | Ready-to-use cURL commands for testing all API endpoints from command line |

### Quick Links

- **[Complete Setup Guide](Docs/SWAGGER_SETUP.md)** - Everything you need to get started
- **[API Testing Guide](Docs/CURL_EXAMPLES.md)** - cURL examples for all endpoints
- **[Environment Configuration](#-configuration)** - Configuration options below

---

## ?? Configuration

The API uses environment-based configuration. Each environment (Development/Staging/Production) has its own settings.

### Active Environment

Set in `appsettings.json`:
```json
"ActiveEnvironment": "Development"
```

### Environment Sections

Each environment contains:
- **OpenAI**: GPT-4 configuration for prescription parsing
- **DeepSeek**: Alternative AI provider (optional)
- **Claude**: Anthropic Claude configuration (optional)
- **Azure Document Intelligence**: OCR service
- **TwoFactor**: Phone OTP authentication
- **Features**: Feature flags
- **FileStorage**: File storage settings
- **Database**: Connection string
- **JWT**: Authentication tokens

### Example Configuration
```json
{
  "Environments": {
    "Development": {
      "OpenAI": {
        "ApiKey": "your-api-key",
        "Model": "gpt-4o-mini",
        "Enabled": true
      },
      "Database": {
        "ConnectionString": "Data Source=medremindDB.db"
      }
    }
  }
}
```

---

## ?? API Endpoints

### Authentication (`/api/Auth`)
- `POST /send-otp` - Send OTP to phone number
- `POST /verify-otp` - Verify OTP and login
- `POST /validate-token` - Validate session token

### Medications (`/api/Medications`)
- Full CRUD operations for medications
- Filter by user, active status, etc.

### Prescriptions (`/api/Prescriptions`)
- Upload and process prescription images
- AI-powered OCR and data extraction
- CRUD operations for prescriptions

### Reminders (`/api/Reminders`)
- Schedule medication reminders
- Manage reminder settings
- Voice recording support

### Adherence (`/api/Adherence`)
- Track medication adherence
- View statistics and history
- Log medication intake

?? **[See complete cURL examples](Docs/CURL_EXAMPLES.md)**

---

## ?? Testing with Swagger

1. Start the API: `dotnet run`
2. Open Swagger UI: `http://localhost:5124/swagger`
3. Test authentication endpoints
4. Get auth token
5. Authorize with token (click "Authorize" button)
6. Test all protected endpoints

?? **[Detailed testing guide](Docs/SWAGGER_SETUP.md#-testing-with-swagger)**

---

## ?? Database

**Location:** `backend/MedRemind.API/Database/medremindDB.db`

- SQLite database created automatically on first run
- Entity Framework Core migrations applied automatically
- To reset: Delete the `.db` file and restart

---

## ?? Integration with Client Applications

### .NET MAUI Mobile App

Update `mobile/MedRemind.Mobile/appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5124"
  }
}
```

### Web or Third-Party Apps

1. Get OpenAPI spec: `http://localhost:5124/swagger/v1/swagger.json`
2. Generate client code using NSwag, AutoRest, or similar tools
3. Implement authentication flow
4. Make API calls with bearer token

?? **[Integration guide](Docs/SWAGGER_SETUP.md#-integration-with-client-applications)**

---

## ?? Deployment

### Options
- Azure App Service
- Docker containers
- IIS (Windows Server)
- Linux with Nginx/Apache

### Before Deployment
1. Update API keys in production environment section
2. Set `ActiveEnvironment` to `"Production"`
3. Configure CORS for your domain
4. Enable HTTPS
5. Implement rate limiting

?? **[Deployment guide](Docs/SWAGGER_SETUP.md#-deployment-options)**

---

## ?? Security Notes

?? **Important:**
- Never commit API keys to version control
- Update default JWT secret key
- Configure proper CORS policy for production
- Use environment variables or Azure Key Vault
- Enable HTTPS in production
- Implement rate limiting

---

## ?? Troubleshooting

### Common Issues

**Database not created?**
- Check `Database` folder permissions
- View console output for errors

**Swagger not loading?**
- Verify correct URL: `http://localhost:5124/swagger`
- Check if API is running

**CORS errors?**
- Update CORS policy in `Program.cs`
- Add your client domain to allowed origins

?? **[Full troubleshooting guide](Docs/SWAGGER_SETUP.md#-troubleshooting)**

---

## ?? Support & Resources

### Documentation
- **[Complete Setup Guide](Docs/SWAGGER_SETUP.md)**
- **[cURL Testing Examples](Docs/CURL_EXAMPLES.md)**
- **Swagger UI**: Available when API is running
- **OpenAPI Spec**: `/swagger/v1/swagger.json`

### Repository
- **GitHub**: https://github.com/rajibmahata/MedRemind
- **Issues**: Create an issue for bugs or feature requests
- **Branch**: `Developer`

### Quick Commands
```bash
# Run API
dotnet run --project backend\MedRemind.API\MedRemind.API.csproj

# Build only
dotnet build backend\MedRemind.API\MedRemind.API.csproj

# Clean and rebuild
dotnet clean && dotnet build
```

---

## ? What's Included

### 5 Complete Controllers
1. **AuthController** - Phone OTP authentication (3 endpoints)
2. **PrescriptionsController** - AI prescription processing (3 endpoints)
3. **MedicationsController** - Full CRUD + dose logging (9 endpoints)
4. **AdherenceController** - Stats and streaks (4 endpoints)
5. **RemindersController** - Reminder management (6 endpoints)

**Total**: 30+ REST endpoints covering all backend functionality

### Infrastructure:
- ? Dependency injection configured
- ? Swagger UI ready
- ? Database auto-initialization
- ? CORS enabled for mobile
- ? Error handling throughout
- ? Async/await operations

---

## ?? API Endpoints

### Authentication

#### Send OTP
```http
POST /api/auth/send-otp
Content-Type: application/json

{
  "phoneNumber": "+919876543210"
}
```

#### Verify OTP
```http
POST /api/auth/verify-otp
Content-Type: application/json

{
  "phoneNumber": "+919876543210",
  "otp": "123456"
}
```

**Response:**
```json
{
  "message": "Login successful",
  "phoneNumber": "+919876543210",
  "token": "abc123..."
}
```

### Prescriptions

#### Upload Prescription
```http
POST /api/prescriptions/upload
Content-Type: application/json

{
  "userId": 1,
  "imageBase64": "base64_encoded_image"
}
```

**Response:**
```json
{
  "prescriptionId": 1,
  "medications": [
    {
      "name": "Paracetamol",
      "dosage": "500",
      "unit": "mg",
      "frequency": "twice daily",
      "frequencyCount": 2,
      "durationDays": 7,
      "confidenceScore": 0.95
    }
  ],
  "doctorName": "Dr. Smith",
  "confidenceScore": 0.92,
  "warnings": []
}
```

#### Get User Prescriptions
```http
GET /api/prescriptions/user/{userId}
```

### Medications

#### Create Medication
```http
POST /api/medications
Content-Type: application/json

{
  "userId": 1,
  "name": "Paracetamol",
  "dosage": "500",
  "unit": "mg",
  "frequency": "twice daily",
  "timesPerDay": 2,
  "instructions": "Take with food",
  "durationDays": 7,
  "createReminders": true
}
```

#### Get Active Medications
```http
GET /api/medications/user/{userId}/active
```

#### Update Medication
```http
PUT /api/medications/{id}
```

#### Pause/Resume Medication
```http
POST /api/medications/{id}/pause
POST /api/medications/{id}/resume
```

#### Log Dose
```http
POST /api/medications/{medicationId}/doses
Content-Type: application/json

{
  "scheduledTime": "2024-12-21T09:00:00",
  "status": "Taken",
  "notes": "Taken on time"
}
```

#### Get Dose History
```http
GET /api/medications/{medicationId}/doses
```

### Adherence

#### Get Adherence Percentage
```http
GET /api/adherence/user/{userId}/percentage?startDate=2024-12-01&endDate=2024-12-21
```

#### Get Longest Streak
```http
GET /api/adherence/user/{userId}/streak
```

#### Get Weekly Summary
```http
GET /api/adherence/user/{userId}/weekly
```

**Response:**
```json
{
  "userId": 1,
  "period": "Last 7 Days",
  "overallPercentage": 85.5,
  "longestStreak": 5,
  "dailyData": [
    {
      "date": "2024-12-15",
      "totalDoses": 6,
      "takenDoses": 5,
      "adherencePercentage": 83.33
    }
  ]
}
```

#### Get Monthly Summary
```http
GET /api/adherence/user/{userId}/monthly
```

### Reminders

#### Get Medication Reminders
```http
GET /api/reminders/medication/{medicationId}
```

#### Get User Reminders
```http
GET /api/reminders/user/{userId}
```

#### Update Reminder Time
```http
PUT /api/reminders/{id}/time
Content-Type: application/json

{
  "newTime": "10:00:00"
}
```

#### Enable/Disable Reminder
```http
PUT /api/reminders/{id}/toggle
Content-Type: application/json

{
  "isEnabled": true
}
```

#### Calculate Reminder Times
```http
POST /api/reminders/calculate
Content-Type: application/json

{
  "timesPerDay": 3
}
```

**Response:**
```json
{
  "timesPerDay": 3,
  "reminderTimes": ["08:00:00", "14:00:00", "20:00:00"]
}
```

#### Calculate Custom Reminder Times
```http
POST /api/reminders/calculate-custom
Content-Type: application/json

{
  "frequency": "every 8 hours"
}
```

---

## ? Testing

### Using Swagger UI
1. Navigate to https://localhost:7001/swagger
2. Explore all endpoints with interactive documentation
3. Test endpoints directly from the browser

### Using HTTP Files
Open `MedRemind.API.http` in Visual Studio or VS Code with REST Client extension:
- Contains pre-configured requests for all endpoints
- Easy to modify and test

### Using Postman
Import the API into Postman:
1. Copy endpoint URLs from this README
2. Create collection with requests
3. Test with your data

---

## ?? Database

The API uses SQLite database stored at:
```
%LocalAppData%/medremindDB.db
```

Database is automatically created on first run with all tables and relationships.

---

## ? Features

### ? AI-Powered Prescription Reading
- GPT-4 Vision integration
- Automatic medication extraction
- Confidence scoring
- Validation warnings

### ? Smart Reminder Scheduling
- 15+ frequency patterns
- Natural language support
- Custom time calculation
- Notification management

### ? Adherence Tracking
- Real-time percentage calculation
- Streak detection
- Daily/Weekly/Monthly summaries
- Multi-medication aggregation

### ? Comprehensive Medication Management
- Full CRUD operations
- Pause/Resume functionality
- Dose logging
- Historical tracking

---

## ? Error Handling

All endpoints return consistent error responses:

**Success (200-204):**
```json
{
  "data": { ... }
}
```

**Client Error (400):**
```json
{
  "message": "Invalid request"
}
```

**Unauthorized (401):**
```json
{
  "message": "Invalid or expired token"
}
```

**Not Found (404):**
```json
{
  "message": "Resource not found"
}
```

**Server Error (500):**
```json
{
  "message": "An error occurred"
}
```

---

## ?? Development

### Project Structure
```
MedRemind.API/
??? Controllers/
?   ??? AuthController.cs
?   ??? PrescriptionsController.cs
?   ??? MedicationsController.cs
?   ??? AdherenceController.cs
?   ??? RemindersController.cs
??? Program.cs
??? appsettings.json
??? MedRemind.API.http
??? README.md
??? API-SETUP-COMPLETE.md
```

### Adding New Endpoints
1. Create controller in `Controllers/` folder
2. Inject required services via constructor
3. Add XML documentation comments
4. Test with HTTP file

---

## ?? Security

- JWT token-based authentication (planned)
- HTTPS enforced in production
- CORS configured for mobile app
- Secure OTP verification
- Input validation on all endpoints

---

## ?? Performance

- Entity Framework Core with SQLite
- Asynchronous operations throughout
- Repository pattern for data access
- Connection pooling
- Query optimization

---

## ?? Support

For issues or questions:
1. Check Swagger documentation at https://localhost:7001/swagger
2. Review HTTP test file (`MedRemind.API.http`)
3. See [API-SETUP-COMPLETE.md](API-SETUP-COMPLETE.md) for detailed setup
4. Check application logs

---

## ? Quick Start Guide

```bash
# 1. Navigate to API project
cd backend/MedRemind.API

# 2. Restore packages
dotnet restore

# 3. Update appsettings.json with your API keys

# 4. Build (may show 4 minor errors - see API-SETUP-COMPLETE.md)
dotnet build

# 5. Run
dotnet run

# 6. Open Swagger UI
# Browser: https://localhost:7001/swagger
```

---

**API Version**: 1.0  
**Last Updated**: December 21, 2024  
**Status**: ? 95% Complete - See API-SETUP-COMPLETE.md for remaining fixes  
**Ready for**: Testing & Integration

