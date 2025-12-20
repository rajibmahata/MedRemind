using Microsoft.Extensions.Logging;
using MedRemind.Services;
using MedRemind.ViewModels;
using MedRemind.Pages;

namespace MedRemind;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register services
		builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddSingleton<IOcrService, MockOcrService>();
		builder.Services.AddSingleton<INotificationService, MockNotificationService>();

		// Register ViewModels
		builder.Services.AddTransient<MedicationsViewModel>();
		builder.Services.AddTransient<AddMedicationViewModel>();
		builder.Services.AddTransient<PrescriptionReaderViewModel>();

		// Register Pages
		builder.Services.AddTransient<MedicationsPage>();
		builder.Services.AddTransient<AddMedicationPage>();
		builder.Services.AddTransient<PrescriptionReaderPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
