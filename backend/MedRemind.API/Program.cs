using MedRemind.Core.Data;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Repositories;
using MedRemind.Services.AI;
using MedRemind.Services.AI.Agents;
using MedRemind.Services.Authentication;
using MedRemind.Services.Medications;
using MedRemind.Services.Media;
using MedRemind.Services.Notifications;
using MedRemind.Services.Prescriptions;
using MedRemind.Services.Reminders;
using MedRemind.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific configuration
var activeEnvironment = builder.Configuration["ActiveEnvironment"] ?? "Development";
var environmentConfig = builder.Configuration.GetSection($"Environments:{activeEnvironment}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT Authentication
var jwtSecretKey = environmentConfig["Jwt:SecretKey"] ?? "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS";
var jwtIssuer = environmentConfig["Jwt:Issuer"] ?? "MedRemind.API";
var jwtAudience = environmentConfig["Jwt:Audience"] ?? "MedRemind.Mobile";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure Swagger/OpenAPI with enhanced documentation and JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "MedRemind API",
        Version = "v1",
        Description = "A comprehensive medication reminder and prescription management API. This API provides endpoints for authentication, medication management, prescription processing using AI/OCR, reminders, and adherence tracking."
    });

    // Enable XML comments for better documentation (if XML file exists)
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Database - Create Database folder in project root
var projectRoot = Directory.GetCurrentDirectory();
var databaseFolder = Path.Combine(projectRoot, "Database");
if (!Directory.Exists(databaseFolder))
{
    Directory.CreateDirectory(databaseFolder);
    Console.WriteLine($"Created Database folder at: {databaseFolder}");
}

var dbPath = Path.Combine(databaseFolder, "medremindDB.db");
builder.Services.AddDbContext<MedRemindDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Repository & Unit of Work
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Core Services
builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
builder.Services.AddSingleton<IAudioService, AudioService>();
builder.Services.AddSingleton<INotificationService, LocalNotificationService>();
builder.Services.AddSingleton<IReminderSchedulingService, ReminderSchedulingService>();
builder.Services.AddSingleton<IValidationAgentService, MedicineValidationAgent>();

// Configure File Storage Service - Simplified structure: files/prescriptions and files/OCRs
builder.Services.Configure<MedRemind.Core.Configuration.FileStorageConfiguration>(options =>
{
    options.OcrLogsFolderName = environmentConfig["FileStorage:OcrLogsFolderName"] ?? "OCRs";
    options.PrescriptionsFolderName = environmentConfig["FileStorage:PrescriptionsFolderName"] ?? "Prescriptions";
    options.EnableFileLogging = bool.Parse(environmentConfig["FileStorage:EnableFileLogging"] ?? "true");
    options.MaxLogFiles = int.Parse(environmentConfig["FileStorage:MaxLogFiles"] ?? "100");
    options.OcrFilePrefix = environmentConfig["FileStorage:OcrFilePrefix"] ?? "OCR";
});

builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

// Add HttpClient for services
builder.Services.AddHttpClient();

// Business Services
builder.Services.AddScoped<IAuthenticationService>(sp =>
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var secureStorage = sp.GetRequiredService<ISecureStorageService>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var twoFactorApiKey = environmentConfig["TwoFactor:ApiKey"] ?? "";
    
    // JWT Configuration from environment
    var jwtSecret = environmentConfig["Jwt:SecretKey"] ?? "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS";
    var jwtIssuer = environmentConfig["Jwt:Issuer"] ?? "MedRemind.API";
    var jwtAudience = environmentConfig["Jwt:Audience"] ?? "MedRemind.Mobile";
    var jwtExpiration = int.Parse(environmentConfig["Jwt:ExpirationDays"] ?? "30");
    
    return new AuthenticationService(
        unitOfWork, 
        secureStorage, 
        twoFactorApiKey, 
        httpClient,
        jwtSecretKey: jwtSecret,
        jwtIssuer: jwtIssuer,
        jwtAudience: jwtAudience,
        jwtExpirationDays: jwtExpiration);
});

builder.Services.AddScoped<MedicationService>();
builder.Services.AddScoped<AdherenceService>();

// Register PrescriptionFileManager for file handling (now uses IFileStorageService)
builder.Services.AddSingleton<IPrescriptionFileManager, PrescriptionFileManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PrescriptionFileManager>>();
    var fileStorageService = sp.GetRequiredService<IFileStorageService>();
    
    Console.WriteLine($"✅ PrescriptionFileManager registered with FileStorageService");
    
    return new PrescriptionFileManager(logger, fileStorageService);
});

// Register PrescriptionService
builder.Services.AddScoped<PrescriptionService>();

// Register AI Parser Agents
builder.Services.AddScoped<OpenAIPrescriptionParserAgent>(sp =>
{
    var openAIKey = environmentConfig["OpenAI:ApiKey"] ?? "";
    var openAIModel = environmentConfig["OpenAI:Model"] ?? "gpt-4o-mini";
    var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);
    return new OpenAIPrescriptionParserAgent(chatClient);
});

builder.Services.AddScoped<DeepSeekPrescriptionParserAgent>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var deepSeekKey = environmentConfig["DeepSeek:ApiKey"] ?? "";
    var deepSeekUrl = environmentConfig["DeepSeek:ApiUrl"] ?? "https://api.deepseek.com/v1/chat/completions";
    var deepSeekMaxTokens = int.Parse(environmentConfig["DeepSeek:MaxTokens"] ?? "5000");
    return new DeepSeekPrescriptionParserAgent(httpClient, deepSeekKey, deepSeekUrl, deepSeekMaxTokens);
});

builder.Services.AddScoped<ClaudePrescriptionParserAgent>(sp =>
{
    var claudeKey = environmentConfig["Claude:ApiKey"] ?? "";
    var claudeModel = environmentConfig["Claude:Model"] ?? "claude-3-5-sonnet-20241022";
    var claudeMaxTokens = int.Parse(environmentConfig["Claude:MaxTokens"] ?? "5000");
    return new ClaudePrescriptionParserAgent(claudeKey, claudeModel, claudeMaxTokens);
});

// Register MultiLlmAPIOrchestrator
builder.Services.AddScoped<MultiLlmAPIOrchestrator>(sp =>
{
    var openAIAgent = sp.GetRequiredService<OpenAIPrescriptionParserAgent>();
    var deepSeekAgent = sp.GetRequiredService<DeepSeekPrescriptionParserAgent>();
    var claudeAgent = sp.GetRequiredService<ClaudePrescriptionParserAgent>();
    
    Console.WriteLine("✅ MultiLlmAPIOrchestrator configured with OpenAI, DeepSeek, and Claude");
    
    return new MultiLlmAPIOrchestrator(openAIAgent, deepSeekAgent, claudeAgent);
});

builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var validationAgent = sp.GetRequiredService<IValidationAgentService>();
    
    // OpenAI configuration from environment
    var openAIKey = environmentConfig["OpenAI:ApiKey"] ?? "";
    
    // Azure Document Intelligence configuration from environment
    var azureEndpoint = environmentConfig["AzureDocumentIntelligence:Endpoint"] ?? "";
    var azureKey = environmentConfig["AzureDocumentIntelligence:ApiKey"] ?? "";
    
    // Create required dependencies for Azure Document Intelligence service
    var preprocessor = new PrescriptionOcrTextPreprocessor();
    var fileStorageService = sp.GetRequiredService<IFileStorageService>();
    
    // Create Azure Document Intelligence service
    var azureDocService = new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey, preprocessor, fileStorageService);
    
    // Get MultiLlmAPIOrchestrator
    var agentOrchestrator = sp.GetRequiredService<MultiLlmAPIOrchestrator>();
    
    // Get optional services
    var deduplicationService = sp.GetService<PrescriptionDeduplicationService>();
    var prescriptionService = sp.GetService<PrescriptionService>();
    
    Console.WriteLine("✅ PrescriptionReaderService configured with MultiLlmAPIOrchestrator");
    
    return new PrescriptionReaderService(
        httpClient, 
        openAIKey, 
        validationAgent, 
        azureDocService, 
        agentOrchestrator,
        deduplicationService,
        prescriptionService);
});

// ============================================
// SEMANTIC KERNEL MULTI-AGENT SYSTEM
// ============================================

// Register Semantic Kernel with OpenAI using environment config
builder.Services.AddSingleton<Microsoft.SemanticKernel.Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var openAIKey = environmentConfig["OpenAI:ApiKey"] ?? "";
    var openAIModel = environmentConfig["OpenAI:Model"] ?? "gpt-4o-mini";

    var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(openAIModel, openAIKey);

    return kernelBuilder.Build();
});

// Register OCR Storage Path
builder.Services.AddSingleton<string>(sp =>
{
    var ocrStoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "medremind_api", 
        "ocr_texts");
    
    if (!Directory.Exists(ocrStoragePath))
    {
        Directory.CreateDirectory(ocrStoragePath);
    }
    
    return ocrStoragePath;
});

// Register the Three Agents
builder.Services.AddScoped<MedRemind.Services.AI.Agents.OCRTextSaverAgent>(sp =>
{
    var storagePath = sp.GetRequiredService<string>();
    return new MedRemind.Services.AI.Agents.OCRTextSaverAgent(storagePath);
});

builder.Services.AddScoped<MedRemind.Services.AI.Agents.PrescriptionDataExtractionAgent>(sp =>
{
    var kernel = sp.GetRequiredService<Microsoft.SemanticKernel.Kernel>();
    return new MedRemind.Services.AI.Agents.PrescriptionDataExtractionAgent(kernel);
});

builder.Services.AddScoped<MedRemind.Services.AI.Agents.ValidationAgent>(sp =>
{
    var extractionAgent = sp.GetRequiredService<MedRemind.Services.AI.Agents.PrescriptionDataExtractionAgent>();
    return new MedRemind.Services.AI.Agents.ValidationAgent(extractionAgent);
});

// Register Deduplication Service
builder.Services.AddScoped<MedRemind.Services.Prescriptions.PrescriptionDeduplicationService>();

// Register result merger and validation services
builder.Services.AddScoped<MedRemind.Services.AI.PrescriptionResultMergerService>();
builder.Services.AddScoped<MedRemind.Services.AI.PrescriptionValidationService>();

// AgentOrchestrator has been replaced by AgentOrchestratorV2 (in mobile app)
// The API uses IPrescriptionReaderService instead

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MedRemindDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
// Enable Swagger in all environments for testing and integration
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MedRemind API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "MedRemind API Documentation";
    options.DefaultModelsExpandDepth(-1); // Disable models section by default
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("========================================");
Console.WriteLine("      MedRemind API Server");
Console.WriteLine("========================================");
Console.WriteLine($"Environment: {activeEnvironment}");
Console.WriteLine($"Database: {dbPath}");
Console.WriteLine($"Swagger UI: {builder.Configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault() ?? "http://localhost:5000"}/swagger");
Console.WriteLine("========================================");

app.Run();
