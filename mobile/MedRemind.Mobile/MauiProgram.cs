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
        builder.Services.AddDbContext<MedRemindDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Configuration Service (must be registered first)
        builder.Services.AddSingleton<IConfigurationService, SecureConfigurationService>();
        builder.Services.AddSingleton<IEnvironmentConfigService, EnvironmentConfigService>();

        // Repository & Unit of Work
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IBiometricService, BiometricService>();
        builder.Services.AddSingleton<IReminderSchedulingService, ReminderSchedulingService>();
        builder.Services.AddSingleton<IValidationAgentService, MedicineValidationAgent>();

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

        // Register HttpClient with configuration-based timeout
        builder.Services.AddSingleton<HttpClient>(sp =>
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30) // Default, can be overridden
            };
            return httpClient;
        });

        // Register OpenAIPrescriptionReaderService with embedded configuration
        builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var validationAgent = sp.GetRequiredService<IValidationAgentService>();
            
            // Load API key from embedded configuration
            var apiKey = EmbeddedConfigurationLoader.GetOpenAIApiKey();
            
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("_KEY_HERE"))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ WARNING: OpenAI API key not configured in appsettings.json");
            }
            else
            {
                var activeEnv = EmbeddedConfigurationLoader.LoadConfiguration().ActiveEnvironment;
                System.Diagnostics.Debug.WriteLine($"✅ OpenAI API key loaded from embedded config for environment: {activeEnv}");
            }
            
            return new OpenAIPrescriptionReaderService(httpClient, apiKey, validationAgent);
        });

        builder.Services.AddScoped<MedicationService>();
        builder.Services.AddScoped<AdherenceService>();
        builder.Services.AddScoped<PrescriptionService>();

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
