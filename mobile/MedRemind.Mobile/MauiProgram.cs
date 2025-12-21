using CommunityToolkit.Maui;
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

        // Repository & Unit of Work
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IAudioService, AudioService>();
        builder.Services.AddSingleton<INotificationService, LocalNotificationService>();
        builder.Services.AddSingleton<IReminderSchedulingService, ReminderSchedulingService>();
        builder.Services.AddSingleton<IValidationAgentService, MedicineValidationAgent>();
        
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        builder.Services.AddScoped<IPrescriptionReaderService, OpenAIPrescriptionReaderService>();
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

        return app;
    }
}
