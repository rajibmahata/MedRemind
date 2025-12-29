using CommunityToolkit.Maui;
using MedRemind.Core.Configuration;
using MedRemind.Core.Data;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Repositories;
using MedRemind.Mobile.Services;
using MedRemind.Services.AI;
using MedRemind.Services.Authentication;
using MedRemind.Services.Medications;
using MedRemind.Services.Media;
using MedRemind.Services.Notifications;
using MedRemind.Services.Prescriptions;
using MedRemind.Services.Reminders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MedRemind.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "medremind.db");
        System.Diagnostics.Debug.WriteLine($"✅ Database Path: {dbPath}");
        builder.Services.AddDbContext<MedRemindDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Configuration Service (must be registered first)
        builder.Services.AddSingleton<IConfigurationService, SecureConfigurationService>();
        builder.Services.AddSingleton<IEnvironmentConfigService, EnvironmentConfigService>();

        // Repository & Unit of Work
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services - Use MAUI-specific SecureStorage implementation
        builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();
        builder.Services.AddSingleton<IBiometricService, BiometricService>();
        builder.Services.AddSingleton<IReminderSchedulingService, ReminderSchedulingService>();
        builder.Services.AddSingleton<IValidationAgentService, MedicineValidationAgent>();
        builder.Services.AddSingleton<INotificationService, LocalNotificationService>();

        // Register AuthenticationService with 2Factor API key from embedded config
        builder.Services.AddScoped<IAuthenticationService>(sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var secureStorage = sp.GetRequiredService<ISecureStorageService>();
            var httpClient = sp.GetRequiredService<HttpClient>();
            
            // Load 2Factor configuration from embedded configuration
            var twoFactorApiKey = EmbeddedConfigurationLoader.GetTwoFactorApiKey();
            var sendOtpUrl = EmbeddedConfigurationLoader.GetTwoFactorSendOtpUrl();
            var verifyOtpUrl = EmbeddedConfigurationLoader.GetTwoFactorVerifyOtpUrl();
            var otpTemplate = EmbeddedConfigurationLoader.GetTwoFactorOtpTemplate();
            
            if (string.IsNullOrEmpty(twoFactorApiKey) || twoFactorApiKey.Contains("_KEY_HERE"))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: 2Factor API key not configured in appsettings.json");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ 2Factor API configured:");
                System.Diagnostics.Debug.WriteLine($"   API Key: {twoFactorApiKey.Substring(0, 8)}...");
                System.Diagnostics.Debug.WriteLine($"   Send URL: {sendOtpUrl}");
                System.Diagnostics.Debug.WriteLine($"   Verify URL: {verifyOtpUrl}");
                System.Diagnostics.Debug.WriteLine($"   Template: {otpTemplate}");
            }
            
            return new AuthenticationService(unitOfWork, secureStorage, twoFactorApiKey, httpClient, 
                sendOtpUrl, verifyOtpUrl, otpTemplate);
        });

        // Register HttpClient with Android-optimized configuration
        builder.Services.AddSingleton<HttpClient>(sp =>
        {
            // Configure HttpClientHandler for better Android compatibility
            var handler = new HttpClientHandler
            {
#if DEBUG
                // Allow all SSL certificates in development (Android emulator issues)
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ SSL Certificate validation warning: {sslPolicyErrors}");
                        System.Diagnostics.Debug.WriteLine($"   Certificate Subject: {cert?.Subject}");
                        System.Diagnostics.Debug.WriteLine($"   Certificate Issuer: {cert?.Issuer}");
                        System.Diagnostics.Debug.WriteLine($"   Valid From: {cert?.NotBefore}");
                        System.Diagnostics.Debug.WriteLine($"   Valid To: {cert?.NotAfter}");
                        
                        // Log chain errors
                        if (chain != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"   Chain Status Count: {chain.ChainStatus.Length}");
                            foreach (var status in chain.ChainStatus)
                            {
                                System.Diagnostics.Debug.WriteLine($"      - {status.Status}: {status.StatusInformation}");
                            }
                        }
                        
                        // Accept all certificates in DEBUG mode for emulator compatibility
                        System.Diagnostics.Debug.WriteLine("   ✅ Accepting certificate in DEBUG mode");
                        return true;
                    }
                    return true;
                },
#else
                // Production: Use default SSL validation
                CheckCertificateRevocationList = true,
#endif
                // Enable automatic decompression
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                
                // Connection settings
                MaxConnectionsPerServer = 10,
                UseDefaultCredentials = false,
                
                // Disable proxy for emulator
                UseProxy = false,
                
                // SSL/TLS Protocol settings
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30) // Reduced from 120s for faster failure detection
            };

            // Add default headers
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MedRemind-Mobile/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");

            System.Diagnostics.Debug.WriteLine("✅ HttpClient configured with Android optimizations");
            System.Diagnostics.Debug.WriteLine($"   Timeout: {httpClient.Timeout.TotalSeconds}s");
            System.Diagnostics.Debug.WriteLine($"   SSL Protocols: TLS 1.2, TLS 1.3");
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"   SSL Validation: Custom (Accept All in DEBUG)");
#else
            System.Diagnostics.Debug.WriteLine($"   SSL Validation: Default (Production)");
#endif

            return httpClient;
        });

        // Register OpenAIPrescriptionReaderService with embedded configuration
        builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var validationAgent = sp.GetRequiredService<IValidationAgentService>();
            
            // Load API keys from embedded configuration
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var openAIKey = config.OpenAI.ApiKey;
            var openAIModel = config.OpenAI.Model; // NEW: Get model from config
            var azureEndpoint = config.AzureDocumentIntelligence.Endpoint;
            var azureKey = config.AzureDocumentIntelligence.ApiKey;
            
            if (string.IsNullOrEmpty(openAIKey) || openAIKey.Contains("_KEY_HERE"))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: OpenAI API key not configured in appsettings.json");
            }
            else
            {
                var activeEnv = EmbeddedConfigurationLoader.LoadConfiguration().ActiveEnvironment;
                System.Diagnostics.Debug.WriteLine($"✅ OpenAI API key loaded from embedded config for environment: {activeEnv}");
                System.Diagnostics.Debug.WriteLine($"   Model: {openAIModel}"); // Log configured model
            }
            
            if (string.IsNullOrEmpty(azureEndpoint) || string.IsNullOrEmpty(azureKey))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: Azure Document Intelligence not configured");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Azure Document Intelligence configured: {azureEndpoint}");
            }
            
            // Create Azure Document Intelligence service
            var azureDocService = new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey);
            
            // Create ChatClient for OpenAI with configured model
            var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey); // Use config model
            System.Diagnostics.Debug.WriteLine($"✅ OpenAI ChatClient initialized with model: {openAIModel}");
            
            // Create Medical Prescription Parser Agent with ChatClient
            var parserAgent = new MedicalPrescriptionParserAgent(chatClient);
            System.Diagnostics.Debug.WriteLine($"✅ Medical Parser Agent initialized");
            
            return new OpenAIPrescriptionReaderService(httpClient, openAIKey, validationAgent, azureDocService, parserAgent, openAIModel); // Pass model
        });

        // Register AzureDocumentIntelligenceService separately for injection into ViewModels
        builder.Services.AddScoped<AzureDocumentIntelligenceService>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var azureEndpoint = config.AzureDocumentIntelligence.Endpoint;
            var azureKey = config.AzureDocumentIntelligence.ApiKey;
            
            System.Diagnostics.Debug.WriteLine($"✅ Registering standalone Azure Document Intelligence Service");
            return new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey);
        });

        // ============================================
        // SEMANTIC KERNEL MULTI-AGENT SYSTEM
        // ============================================
        
        // Register Semantic Kernel with OpenAI
        builder.Services.AddSingleton<Microsoft.SemanticKernel.Kernel>(sp =>
        {
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var openAIKey = config.OpenAI.ApiKey;
            var openAIModel = config.OpenAI.Model;

            var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(openAIModel, openAIKey);

            var kernel = kernelBuilder.Build();
            System.Diagnostics.Debug.WriteLine($"✅ Semantic Kernel initialized for multi-agent system");
            System.Diagnostics.Debug.WriteLine($"   Model: {openAIModel}");
            return kernel;
        });

        // Register OCR Storage Path
        builder.Services.AddSingleton<string>(sp =>
        {
            var ocrStoragePath = Path.Combine(FileSystem.AppDataDirectory, "ocr_texts");
            if (!Directory.Exists(ocrStoragePath))
            {
                Directory.CreateDirectory(ocrStoragePath);
            }
            System.Diagnostics.Debug.WriteLine($"✅ OCR Storage Path: {ocrStoragePath}");
            return ocrStoragePath;
        });

        // ============================================
        // PRESCRIPTION DEDUPLICATION SERVICE
        // ============================================
        
        // Register deduplication service for duplicate prescription detection
        builder.Services.AddScoped<MedRemind.Services.Prescriptions.PrescriptionDeduplicationService>();
        
        // Register result merger and validation services
        builder.Services.AddScoped<MedRemind.Services.AI.PrescriptionResultMergerService>();
        builder.Services.AddScoped<MedRemind.Services.AI.PrescriptionValidationService>();
        
        System.Diagnostics.Debug.WriteLine($"✅ Prescription services registered (Deduplication, Merger, Validation)");

        // ============================================
        // AI PARSER AGENTS (Priority-Based Multi-Parser System)
        // ============================================
        
        // Register DeepSeek Parser (if enabled)
        builder.Services.AddScoped<MedRemind.Services.AI.DeepSeekPrescriptionParserAgent?>(sp =>
        {
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var deepSeek = config.DeepSeek;
            
            if (deepSeek != null && deepSeek.Enabled && !string.IsNullOrEmpty(deepSeek.ApiKey) && !deepSeek.ApiKey.Contains("_KEY_HERE"))
            {
                var httpClient = sp.GetRequiredService<HttpClient>();
                System.Diagnostics.Debug.WriteLine($"✅ DeepSeek Parser: Enabled (Priority {deepSeek.Priority})");
                System.Diagnostics.Debug.WriteLine($"   API URL: {deepSeek.ApiUrl}");
                return new MedRemind.Services.AI.DeepSeekPrescriptionParserAgent(
                    httpClient, 
                    deepSeek.ApiKey,
                    deepSeek.ApiUrl);
            }
            
            System.Diagnostics.Debug.WriteLine($"❌ DeepSeek Parser: Disabled");
            return null;
        });
        
        // Register Claude Parser (if enabled) - Note: requires Anthropic SDK
        builder.Services.AddScoped<MedRemind.Services.AI.ClaudePrescriptionParserAgent?>(sp =>
        {
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var claude = config.Claude;
            
            if (claude != null && claude.Enabled && !string.IsNullOrEmpty(claude.ApiKey) && !claude.ApiKey.Contains("_KEY_HERE"))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Claude Parser: Enabled (Priority {claude.Priority})");
                System.Diagnostics.Debug.WriteLine($"⚠️ Note: Claude parser requires ClaudePrescriptionParserAgent implementation");
                // TODO: Implement ClaudePrescriptionParserAgent
                return null; // Not implemented yet
            }
            
            System.Diagnostics.Debug.WriteLine($"❌ Claude Parser: Disabled");
            return null;
        });

        // ============================================
        // AGENT SYSTEM
        // ============================================

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

        // Register Agent Orchestrator with Priority-Based Multi-Parser System
        builder.Services.AddScoped<MedRemind.Services.AI.Agents.AgentOrchestrator>(sp =>
        {
            var ocrSaver = sp.GetRequiredService<MedRemind.Services.AI.Agents.OCRTextSaverAgent>();
            var extraction = sp.GetRequiredService<MedRemind.Services.AI.Agents.PrescriptionDataExtractionAgent>();
            var validation = sp.GetRequiredService<MedRemind.Services.AI.Agents.ValidationAgent>();
            
            // Get config
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            
            // Get OpenAI parser (always enabled)
            var openAIKey = config.OpenAI.ApiKey;
            var openAIModel = config.OpenAI.Model;
            var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);
            var openAIParser = new MedRemind.Services.AI.MedicalPrescriptionParserAgent(chatClient);
            
            // Get optional parsers - Use GetService instead of GetRequiredService
            var deepSeekParser = sp.GetService<MedRemind.Services.AI.DeepSeekPrescriptionParserAgent?>();
            var claudeParser = sp.GetService<MedRemind.Services.AI.ClaudePrescriptionParserAgent?>();
            
            // Get services
            var deduplicationService = sp.GetRequiredService<MedRemind.Services.Prescriptions.PrescriptionDeduplicationService>();
            var mergerService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionResultMergerService>();
            var validationService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionValidationService>();
            
            // Get AI parser configuration
            var parserConfig = config.AIParser ?? new AIParserConfiguration();
            var deepSeekConfig = config.DeepSeek;
            var claudeConfig = config.Claude;
            
            System.Diagnostics.Debug.WriteLine($"✅ Agent Orchestrator: Priority-Based Multi-Parser System");
            System.Diagnostics.Debug.WriteLine($"   DeepSeek: {(deepSeekConfig?.Enabled == true ? $"✅ Priority {deepSeekConfig.Priority}" : "❌")}");
            System.Diagnostics.Debug.WriteLine($"   OpenAI: ✅ Priority {parserConfig.OpenAIPriority}");
            System.Diagnostics.Debug.WriteLine($"   Claude: {(claudeConfig?.Enabled == true ? $"✅ Priority {claudeConfig.Priority}" : "❌")}");
            System.Diagnostics.Debug.WriteLine($"   Skip Claude if complete: {parserConfig.SkipClaudeIfComplete}");
            
            return new MedRemind.Services.AI.Agents.AgentOrchestrator(
                ocrSaver, 
                extraction, 
                validation,
                openAIParser,
                deduplicationService,
                mergerService,
                validationService,
                deepSeekParser,
                claudeParser,
                deepSeekEnabled: deepSeekConfig?.Enabled ?? false,
                claudeEnabled: claudeConfig?.Enabled ?? false,
                skipClaudeIfComplete: parserConfig.SkipClaudeIfComplete,
                deepSeekPriority: deepSeekConfig?.Priority ?? 1,
                openAIPriority: parserConfig.OpenAIPriority,
                claudePriority: claudeConfig?.Priority ?? 3
            );
        });

        // ============================================
        // BUSINESS SERVICES
        // ============================================
        
        // Register MedicationService
        builder.Services.AddScoped<MedicationService>();
        
        // Register AdherenceService  
        builder.Services.AddScoped<AdherenceService>();
        
        // Register PrescriptionService
        builder.Services.AddScoped<PrescriptionService>();
        
        System.Diagnostics.Debug.WriteLine($"✅ Business services registered (Medication, Adherence, Prescription)");

        // ViewModels
        builder.Services.AddTransient<ViewModels.LoginViewModel>();
        builder.Services.AddTransient<ViewModels.HomeViewModel>();
        builder.Services.AddTransient<ViewModels.MedicationsViewModel>();
        builder.Services.AddTransient<ViewModels.PrescriptionUploadViewModel>();
        builder.Services.AddTransient<ViewModels.RemindersViewModel>();
        builder.Services.AddTransient<ViewModels.AdherenceViewModel>();
        builder.Services.AddTransient<ViewModels.SettingsViewModel>();

        // Pages
        builder.Services.AddTransient<Views.LoginPage>();
        builder.Services.AddTransient<Views.HomePage>();
        builder.Services.AddTransient<Views.MedicationsPage>();
        builder.Services.AddTransient<Views.PrescriptionUploadPage>();
        builder.Services.AddTransient<Views.RemindersPage>();
        builder.Services.AddTransient<Views.AdherencePage>();
        builder.Services.AddTransient<Views.SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MedRemindDbContext>();
            context.Database.EnsureCreated();
            
            // Manually create PrescriptionOCRResults table if it doesn't exist
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS PrescriptionOCRResults (
                        Id INTEGER NOT NULL CONSTRAINT PK_PrescriptionOCRResults PRIMARY KEY AUTOINCREMENT,
                        PrescriptionId INTEGER NOT NULL,
                        OCRText TEXT NOT NULL,
                        OCRTextHash TEXT NOT NULL,
                        OpenAIResponse TEXT NULL,
                        ClaudeResponse TEXT NULL,
                        SelectedResponse TEXT NULL,
                        SelectedProvider TEXT NULL,
                        ComparisonScore REAL NOT NULL,
                        ComparisonReason TEXT NULL,
                        MedicationCount INTEGER NOT NULL,
                        DoctorName TEXT NULL,
                        PatientName TEXT NULL,
                        PrescriptionDate TEXT NULL,
                        ProcessedAt TEXT NOT NULL,
                        ProcessingTime TEXT NOT NULL,
                        ProcessingAttempts INTEGER NOT NULL,
                        CONSTRAINT FK_PrescriptionOCRResults_Prescriptions_PrescriptionId 
                            FOREIGN KEY (PrescriptionId) 
                            REFERENCES Prescriptions (Id) 
                            ON DELETE CASCADE
                    )
                ");
                
                context.Database.ExecuteSqlRaw(@"
                    CREATE INDEX IF NOT EXISTS IX_PrescriptionOCRResults_OCRTextHash 
                        ON PrescriptionOCRResults (OCRTextHash)
                ");
                
                context.Database.ExecuteSqlRaw(@"
                    CREATE INDEX IF NOT EXISTS IX_PrescriptionOCRResults_PrescriptionId 
                        ON PrescriptionOCRResults (PrescriptionId)
                ");
                
                System.Diagnostics.Debug.WriteLine("✅ PrescriptionOCRResults table created/verified");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Table creation note: {ex.Message}");
                // Table might already exist, which is fine
            }
        }

        // Initialize configuration on first launch
        Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
                await configService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("✅ Configuration initialized successfully");
                
                // Load and validate embedded configuration
                var embeddedConfig = EmbeddedConfigurationLoader.LoadConfiguration();
                System.Diagnostics.Debug.WriteLine($"✅ Embedded configuration loaded - Active Environment: {embeddedConfig.ActiveEnvironment}");
                
                // Log available environments
                foreach (var env in embeddedConfig.Environments.Keys)
                {
                    var envConfig = embeddedConfig.Environments[env];
                    var hasOpenAI = !string.IsNullOrEmpty(envConfig.OpenAI.ApiKey) && !envConfig.OpenAI.ApiKey.Contains("_KEY_HERE");
                    System.Diagnostics.Debug.WriteLine($"  - {env}: OpenAI configured = {hasOpenAI}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Configuration initialization failed: {ex.Message}");
            }
        }).Wait();

        return app;
    }
}
