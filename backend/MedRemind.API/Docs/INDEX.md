# MedRemind API Documentation

Welcome to the MedRemind API documentation hub. This folder contains all the guides and documentation you need to work with the MedRemind API.

---

## ?? Documentation Index

### ?? Getting Started

| Document | Description | Audience |
|----------|-------------|----------|
| **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** | Complete setup and configuration guide | Developers, DevOps |
| **[JWT_AUTHENTICATION.md](JWT_AUTHENTICATION.md)** | JWT authentication setup and usage | All Developers || **[API_REFERENCE.md]| **[API_REFERENCE.md](API_REFERENCE.md)** | Quick API reference and endpoint guide | All Developers |
| **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** | cURL command examples for testing | Developers, QA Testers |

---

## ?? Document Overview

### 1. [SWAGGER_SETUP.md](SWAGGER_SETUP.md)
**Complete API Setup & Integration Guide**

**Contents:**
- ? Quick start instructions
- ?? Configuration options
- ?? Database setup
- ?? Authentication flow
- ?? Testing with Swagger UI
- ?? Integration with clients (.NET MAUI, Web, Third-party)
- ?? Deployment options (Azure, Docker, IIS)
- ?? Troubleshooting guide
- ?? Security best practices

**Best for:**
- First-time setup
- Understanding the architecture
- Deploying to production
- Troubleshooting issues

---

### 2. [CURL_EXAMPLES.md](CURL_EXAMPLES.md)
**API Testing with cURL Commands**

**Contents:**
- ?? Authentication examples
- ?? Medication CRUD operations
- ?? Prescription upload and management
- ? Reminder operations
- ?? Adherence tracking
- ?? Testing tips for different platforms

**Best for:**
- Quick API testing
- Command-line integration
- CI/CD pipeline testing
- Learning API endpoints

---

## ?? Quick Navigation by Task

### I want to...

#### Set up the API for the first time
?? Start with **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Section "Getting Started"

#### Test API endpoints
?? Use **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Section "Testing with Swagger"
Or **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** for command-line testing

#### Integrate with my mobile app
?? **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Section "Integration with Client Applications"

#### Configure different environments
?? **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Section "Configuration"

#### Deploy to production
?? **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Section "Deployment"

#### Troubleshoot issues
?? **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Section "Troubleshooting"

#### Test from command line
?? **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** - All sections

---

## ?? Quick Links

### Live Resources (when API is running)
- **Swagger UI**: `http://localhost:5124/swagger`
- **OpenAPI Spec**: `http://localhost:5124/swagger/v1/swagger.json`
- **API Base URL**: `http://localhost:5124/api`

### Project Files
- **[Main README](../README.md)** - Project overview
- **[Configuration File](../appsettings.json)** - API settings
- **[Program.cs](../Program.cs)** - Startup configuration

### External Resources
- **GitHub Repository**: https://github.com/rajibmahata/MedRemind
- **OpenAPI Specification**: https://swagger.io/specification/
- **.NET Documentation**: https://docs.microsoft.com/dotnet/

---

## ?? API Endpoints Summary

### Available Controllers

| Controller | Base Path | Endpoints | Documentation |
|------------|-----------|-----------|---------------|
| **Auth** | `/api/Auth` | 3 | Send OTP, Verify OTP, Validate Token |
| **Medications** | `/api/Medications` | 5+ | CRUD + Search |
| **Prescriptions** | `/api/Prescriptions` | 4+ | Upload, Process, CRUD |
| **Reminders** | `/api/Reminders` | 5+ | CRUD + Schedule |
| **Adherence** | `/api/Adherence` | 3+ | Stats, Log, History |

**Total Endpoints**: 20+

See **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** for detailed endpoint examples.

---

## ?? Authentication Flow

1. **Send OTP** ? `POST /api/Auth/send-otp`
2. **Verify OTP** ? `POST /api/Auth/verify-otp` (returns token)
3. **Use Token** ? Include `Authorization: Bearer {token}` in subsequent requests

See **[SWAGGER_SETUP.md](SWAGGER_SETUP.md#-authentication)** for detailed authentication guide.

---

## ?? Key Features

### AI-Powered Prescription Processing
- Azure Document Intelligence OCR
- OpenAI GPT-4 parsing
- Claude fallback support
- DeepSeek alternative
- Automatic medication extraction

### Multi-Environment Support
- Development
- Staging
- Production
- Environment-specific configurations

### Comprehensive API
- RESTful architecture
- OpenAPI/Swagger documentation
- CORS enabled
- Bearer token authentication
- JSON responses

### Database
- SQLite for easy deployment
- Entity Framework Core
- Automatic migrations
- Located in `Database/` folder

---

## ?? Getting Help

### Documentation Issues?
- Check the main **[README.md](../README.md)**
- Review **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** troubleshooting section
- Check Swagger UI for live API documentation

### API Issues?
- Check console logs when running the API
- Verify configuration in `appsettings.json`
- Test with Swagger UI first
- Use cURL examples from **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)**

### Report Bugs
- Create an issue on GitHub: https://github.com/rajibmahata/MedRemind/issues
- Include error messages and logs
- Specify environment (Development/Staging/Production)

---

## ?? Updates & Maintenance

### Keeping Documentation Updated

When making changes to the API:
1. Update relevant documentation files
2. Update Swagger XML comments in code
3. Test all cURL examples
4. Update this INDEX if adding new documents

### Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | Jan 2025 | Initial documentation setup |

---

## ?? Documentation Standards

### File Naming
- Use UPPERCASE for documentation files
- Use underscores for multi-word names
- Use `.md` extension for Markdown files

### Content Standards
- Include code examples
- Provide step-by-step instructions
- Add troubleshooting sections
- Use emojis for visual clarity
- Keep examples up-to-date

---

## ?? Learning Path

**For New Developers:**
1. Read **[Main README](../README.md)** for overview
2. Follow **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** for setup
3. Test with Swagger UI
4. Try **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** commands
5. Integrate with your application

**For QA/Testers:**
1. Review **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Testing section
2. Use **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** for test cases
3. Use Swagger UI for exploratory testing

**For DevOps:**
1. Review **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - Deployment section
2. Check configuration requirements
3. Set up environment-specific settings
4. Configure CI/CD pipelines

---

## ? Checklist for New Users

- [ ] Read main README
- [ ] Configure `appsettings.json`
- [ ] Run the API
- [ ] Access Swagger UI
- [ ] Test authentication flow
- [ ] Test one endpoint from each controller
- [ ] Try cURL examples
- [ ] Integrate with your client app

---

**Happy Coding! ??**

For more information, start with **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** or jump to **[CURL_EXAMPLES.md](CURL_EXAMPLES.md)** for quick testing.

