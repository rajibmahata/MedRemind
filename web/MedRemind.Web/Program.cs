using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MedRemind.Web;
using MedRemind.Web.Services;
using MedRemind.Web.Models;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load API settings from configuration
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();

// Configure HttpClient for API
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiSettings.BaseUrl),
    Timeout = TimeSpan.FromSeconds(apiSettings.Timeout)
});

// Add MudBlazor
builder.Services.AddMudServices();

// Register API Settings
builder.Services.AddSingleton(apiSettings);

// Add Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

await builder.Build().RunAsync();
