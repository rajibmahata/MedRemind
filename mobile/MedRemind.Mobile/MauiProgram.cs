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

        // File Storage Service (with configuration from appsettings.json)
        builder.Services.Configure<FileStorageConfiguration>(options =>
        {
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var fileStorageConfig = config.FileStorage;
            
            if (fileStorageConfig != null)
            {
                options.OcrLogsFolderName = fileStorageConfig.OcrLogsFolderName;
                options.PrescriptionsFolderName = fileStorageConfig.PrescriptionsFolderName;
                options.EnableFileLogging = fileStorageConfig.EnableFileLogging;
                options.MaxLogFiles = fileStorageConfig.MaxLogFiles;
                options.OcrFilePrefix = fileStorageConfig.OcrFilePrefix;
                
                System.Diagnostics.Debug.WriteLine($"✅ FileStorageConfiguration loaded from appsettings.json");
                System.Diagnostics.Debug.WriteLine($"   OCR Logs Folder: {options.OcrLogsFolderName}");
                System.Diagnostics.Debug.WriteLine($"   Prescriptions Folder: {options.PrescriptionsFolderName}");
                System.Diagnostics.Debug.WriteLine($"   File Logging: {options.EnableFileLogging}");
                System.Diagnostics.Debug.WriteLine($"   Max Log Files: {options.MaxLogFiles}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ FileStorage configuration not found in appsettings.json, using defaults");
            }
        });
        builder.Services.AddSingleton<IFileStorageService, MedRemind.Services.Storage.FileStorageService>();
        System.Diagnostics.Debug.WriteLine($"✅ FileStorageService registered");

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
                Timeout = TimeSpan.FromMinutes(15) // Reduced from 15s for faster failure detection
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

        // Register OpenAIPrescriptionReaderService with MultiLlmAPIOrchestrator
        builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var validationAgent = sp.GetRequiredService<IValidationAgentService>();
            
            // Load API keys from embedded configuration
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var openAIKey = config.OpenAI.ApiKey;
            var openAIModel = config.OpenAI.Model;
            var deepSeekKey = config.DeepSeek.ApiKey;
            var deepSeekModel = config.DeepSeek.Model;
            var deepSeekApiUrl = config.DeepSeek.ApiUrl;
            var deepSeekMaxTokens = config.DeepSeek.MaxTokens;
            var claudeKey = config.Claude.ApiKey;
            var claudeModel = config.Claude.Model;
            var claudeMaxTokens = config.Claude.MaxTokens;
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
                System.Diagnostics.Debug.WriteLine($"   Model: {openAIModel}");
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
            var prescriptionOcrTextPreprocessor = sp.GetRequiredService<PrescriptionOcrTextPreprocessor>();
            var fileStorageService = sp.GetRequiredService<IFileStorageService>();
            var azureDocService = new AzureDocumentIntelligenceService(
                httpClient, azureEndpoint, azureKey, prescriptionOcrTextPreprocessor, fileStorageService);
            
            // Create parser agents with correct constructors
            var openAIChatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);
            var openAIAgent = new OpenAIPrescriptionParserAgent(openAIChatClient);
            System.Diagnostics.Debug.WriteLine($"✅ OpenAI Parser Agent initialized with model: {openAIModel}");
            
            var deepSeekAgent = new DeepSeekPrescriptionParserAgent(httpClient, deepSeekKey, deepSeekApiUrl, deepSeekMaxTokens);
            System.Diagnostics.Debug.WriteLine($"✅ DeepSeek Parser Agent initialized");
            
            var claudeAgent = new ClaudePrescriptionParserAgent(claudeKey, claudeModel, claudeMaxTokens);
            System.Diagnostics.Debug.WriteLine($"✅ Claude Parser Agent initialized");
            
            // Create MultiLlmAPIOrchestrator (simple version for mobile)
            var agentOrchestrator = new MedRemind.Services.AI.Agents.MultiLlmAPIOrchestrator(openAIAgent, deepSeekAgent, claudeAgent);
            System.Diagnostics.Debug.WriteLine($"✅ MultiLlmAPIOrchestrator configured with all parsers");
            
            return new PrescriptionReaderService(
                httpClient, 
                openAIKey, 
                validationAgent, 
                azureDocService, 
                agentOrchestrator);
        });

        // Register AzureDocumentIntelligenceService separately for injection into ViewModels
        builder.Services.AddScoped<AzureDocumentIntelligenceService>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var azureEndpoint = config.AzureDocumentIntelligence.Endpoint;
            var azureKey = config.AzureDocumentIntelligence.ApiKey;
            
            System.Diagnostics.Debug.WriteLine($"✅ Registering standalone Azure Document Intelligence Service");
            var prescriptionOcrTextPreprocessor = sp.GetRequiredService<PrescriptionOcrTextPreprocessor>();
            var fileStorageService = sp.GetRequiredService<IFileStorageService>();
            return new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey, prescriptionOcrTextPreprocessor, fileStorageService);
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
                System.Diagnostics.Debug.WriteLine($"   Max Tokens: {deepSeek.MaxTokens}");
                return new MedRemind.Services.AI.DeepSeekPrescriptionParserAgent(
                    httpClient, 
                    deepSeek.ApiKey,
                    deepSeek.ApiUrl,
                    deepSeek.MaxTokens);
            }
            
            System.Diagnostics.Debug.WriteLine($"❌ DeepSeek Parser: Disabled");
            return null;
        });
        
        // Register Claude Parser (if enabled)
        builder.Services.AddScoped<MedRemind.Services.AI.ClaudePrescriptionParserAgent?>(sp =>
        {
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var claude = config.Claude;
            
            if (claude != null && claude.Enabled && !string.IsNullOrEmpty(claude.ApiKey) && !claude.ApiKey.Contains("_KEY_HERE"))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Claude Parser: Enabled (Priority {claude.Priority})");
                System.Diagnostics.Debug.WriteLine($"   Model: {claude.Model}");
                System.Diagnostics.Debug.WriteLine($"   Max Tokens: {claude.MaxTokens}");
                return new MedRemind.Services.AI.ClaudePrescriptionParserAgent(
                    claude.ApiKey,
                    claude.Model,
                    claude.MaxTokens);
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

       
        // ============================================
        // NEW: AGENT ORCHESTRATOR V2 (Dynamic Architecture)
        // ============================================
        
        // Register Memory Cache for prescription caching
        builder.Services.AddMemoryCache();
        
        // Register PrescriptionCacheService
        builder.Services.AddSingleton<MedRemind.Services.AI.PrescriptionCacheService>();

        // Prescription Ocr Text Preprocessor
        builder.Services.AddSingleton<MedRemind.Services.AI.PrescriptionOcrTextPreprocessor>();

        // Register ParserRegistry
        builder.Services.AddSingleton<MedRemind.Services.AI.ParserRegistry>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MedRemind.Services.AI.ParserRegistry>>();
            var registry = new MedRemind.Services.AI.ParserRegistry(logger);
            
            var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
            var parserLogger = sp.GetRequiredService<ILogger<MedRemind.Services.AI.ResilientParser>>();
            
            // Register DeepSeek Parser (if enabled)
            var deepSeekParser = sp.GetService<MedRemind.Services.AI.DeepSeekPrescriptionParserAgent?>();
            if (deepSeekParser != null && config.DeepSeek?.Enabled == true)
            {
                var adapter = new MedRemind.Services.AI.Adapters.DeepSeekParserAdapter(
                    deepSeekParser,
                    isEnabled: true,
                    priority: config.DeepSeek.Priority);
                
                var resilient = new MedRemind.Services.AI.ResilientParser(adapter, parserLogger);
                registry.Register(resilient);
            }
            
            // Register OpenAI Parser (if enabled) - FIX: Check Enabled flag!
            if (config.OpenAI?.Enabled == true)
            {
                var openAIKey = config.OpenAI.ApiKey;
                var openAIModel = config.OpenAI.Model;
                var chatClient = new OpenAI.Chat.ChatClient(openAIModel, openAIKey);
                var openAIParser = new OpenAIPrescriptionParserAgent(chatClient);
                var openAIAdapter = new MedRemind.Services.AI.Adapters.OpenAIParserAdapter(
                    openAIParser,
                    priority: config.AIParser?.OpenAIPriority ?? 2);
                var openAIResilient = new MedRemind.Services.AI.ResilientParser(openAIAdapter, parserLogger);
                registry.Register(openAIResilient);
                
                System.Diagnostics.Debug.WriteLine($"✅ OpenAI Parser: Registered (Priority {config.AIParser?.OpenAIPriority ?? 2})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ OpenAI Parser: Disabled in configuration");
            }
            
            // Register Claude Parser (if enabled)
            var claudeParser = sp.GetService<MedRemind.Services.AI.ClaudePrescriptionParserAgent?>();
            if (claudeParser != null && config.Claude?.Enabled == true)
            {
                var adapter = new MedRemind.Services.AI.Adapters.ClaudeParserAdapter(
                    claudeParser,
                    isEnabled: true,
                    priority: config.Claude.Priority);
                
                var resilient = new MedRemind.Services.AI.ResilientParser(adapter, parserLogger);
                registry.Register(resilient);
            }
            
            return registry;
        });
        
        // Register Agent Orchestrator V2
        builder.Services.AddScoped<MedRemind.Services.AI.AgentOrchestratorV2>(sp =>
        {
            var ocrSaver = sp.GetRequiredService<MedRemind.Services.AI.Agents.OCRTextSaverAgent>();
            var parserRegistry = sp.GetRequiredService<MedRemind.Services.AI.ParserRegistry>();
            var mergerService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionResultMergerService>();
            var validationService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionValidationService>();
            var deduplicationService = sp.GetRequiredService<MedRemind.Services.Prescriptions.PrescriptionDeduplicationService>();
            var cacheService = sp.GetRequiredService<MedRemind.Services.AI.PrescriptionCacheService>();
            var logger = sp.GetRequiredService<ILogger<MedRemind.Services.AI.AgentOrchestratorV2>>();
            
            return new MedRemind.Services.AI.AgentOrchestratorV2(
                ocrSaver,
                parserRegistry,
                mergerService,
                validationService,
                deduplicationService,
                cacheService,
                logger,
                executionMode: MedRemind.Services.AI.AgentOrchestratorV2.ExecutionMode.Parallel);
        });
        
        System.Diagnostics.Debug.WriteLine($"✅ Agent Orchestrator V2 registered with dynamic architecture");

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
