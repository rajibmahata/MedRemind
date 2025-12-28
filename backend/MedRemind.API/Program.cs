using MedRemind.Core.Data;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Repositories;
using MedRemind.Services.AI;
using MedRemind.Services.Authentication;
using MedRemind.Services.Medications;
using MedRemind.Services.Media;
using MedRemind.Services.Notifications;
using MedRemind.Services.Prescriptions;
using MedRemind.Services.Reminders;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "medremind_api.db");
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

// Add HttpClient for services
builder.Services.AddHttpClient();

// Business Services
builder.Services.AddScoped<IAuthenticationService>(sp =>
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var secureStorage = sp.GetRequiredService<ISecureStorageService>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var twoFactorApiKey = config["TwoFactor:ApiKey"] ?? "";
    return new AuthenticationService(unitOfWork, secureStorage, twoFactorApiKey, httpClient);
});

builder.Services.AddScoped<MedicationService>();
builder.Services.AddScoped<AdherenceService>();

builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var validationAgent = sp.GetRequiredService<IValidationAgentService>();
    
    // OpenAI configuration
    var openAIKey = config["OpenAI:ApiKey"] ?? "";
    var openAIModel = config["OpenAI:Model"] ?? "gpt-4o"; // NEW: Get model from config with fallback
    
    // Azure Document Intelligence configuration
    var azureEndpoint = config["AzureDocumentIntelligence:Endpoint"] ?? "";
    var azureKey = config["AzureDocumentIntelligence:ApiKey"] ?? "";
    
    // Create Azure Document Intelligence service
    var azureDocService = new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey);
    
    // Create ChatClient for OpenAI with configured model
    var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey); // Use config model
    
    // Create Medical Prescription Parser Agent with ChatClient
    var parserAgent = new MedicalPrescriptionParserAgent(chatClient);
    
    return new OpenAIPrescriptionReaderService(httpClient, openAIKey, validationAgent, azureDocService, parserAgent, openAIModel); // Pass model
});

// ============================================
// SEMANTIC KERNEL MULTI-AGENT SYSTEM
// ============================================

// Register Semantic Kernel with OpenAI
builder.Services.AddSingleton<Microsoft.SemanticKernel.Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var openAIKey = config["OpenAI:ApiKey"] ?? "";
    var openAIModel = config["OpenAI:Model"] ?? "gpt-4o"; // NEW: Get model from config with fallback

    var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(openAIModel, openAIKey); // Use config model

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

// Register Agent Orchestrator
builder.Services.AddScoped<MedRemind.Services.AI.Agents.AgentOrchestrator>(sp =>
{
    var ocrSaver = sp.GetRequiredService<MedRemind.Services.AI.Agents.OCRTextSaverAgent>();
    var extraction = sp.GetRequiredService<MedRemind.Services.AI.Agents.PrescriptionDataExtractionAgent>();
    var validation = sp.GetRequiredService<MedRemind.Services.AI.Agents.ValidationAgent>();
    
    // Get OpenAI parser
    var config = sp.GetRequiredService<IConfiguration>();
    var openAIKey = config["OpenAI:ApiKey"] ?? "";
    var openAIModel = config["OpenAI:Model"] ?? "gpt-4o";
    var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);
    var openAIParser = new MedRemind.Services.AI.MedicalPrescriptionParserAgent(chatClient);
    
    // Get deduplication service
    var deduplicationService = sp.GetRequiredService<MedRemind.Services.Prescriptions.PrescriptionDeduplicationService>();
    
    // Get merger and validation services
    var mergerService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionResultMergerService>();
    var validationService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionValidationService>();
    
    return new MedRemind.Services.AI.Agents.AgentOrchestrator(
        ocrSaver, 
        extraction, 
        validation,
        openAIParser,
        deduplicationService,
        mergerService,
        validationService
    );
});


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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

Console.WriteLine($"Database location: {dbPath}");
Console.WriteLine("MedRemind API is running!");
Console.WriteLine("Swagger UI: https://localhost:7073/swagger");

app.Run();
