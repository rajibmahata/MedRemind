using MedRemind.Web.Models;
using Microsoft.Extensions.Configuration;

namespace MedRemind.Web.Services;

/// <summary>
/// Service for accessing application configuration
/// </summary>
public interface IConfigurationService
{
    ApiSettings ApiSettings { get; }
    EndpointsConfiguration Endpoints { get; }
    AppSettings AppSettings { get; }
    FeaturesConfiguration Features { get; }
    AuthenticationConfiguration Authentication { get; }
    StorageConfiguration Storage { get; }
    UIConfiguration UI { get; }
    
    string GetApiUrl(string endpoint);
    string GetFullUrl(string endpoint, params object[] parameters);
}

public class ConfigurationService : IConfigurationService
{
    private readonly AppConfiguration _config;

    public ConfigurationService(IConfiguration configuration)
    {
        _config = new AppConfiguration();
        configuration.Bind(_config);
    }

    public ApiSettings ApiSettings => _config.ApiSettings;
    public EndpointsConfiguration Endpoints => _config.Endpoints;
    public AppSettings AppSettings => _config.AppSettings;
    public FeaturesConfiguration Features => _config.Features;
    public AuthenticationConfiguration Authentication => _config.Authentication;
    public StorageConfiguration Storage => _config.Storage;
    public UIConfiguration UI => _config.UI;

    /// <summary>
    /// Get full API URL for an endpoint
    /// </summary>
    public string GetApiUrl(string endpoint)
    {
        return ApiSettings.GetFullUrl(endpoint);
    }

    /// <summary>
    /// Get full URL with parameter replacement
    /// Example: GetFullUrl("/api/users/{id}", 123) => "http://localhost:5000/api/users/123"
    /// </summary>
    public string GetFullUrl(string endpoint, params object[] parameters)
    {
        var url = endpoint;
        
        // Replace {id}, {0}, {1}, etc. with actual parameters
        for (int i = 0; i < parameters.Length; i++)
        {
            url = url.Replace($"{{{i}}}", parameters[i].ToString());
            url = url.Replace("{id}", parameters[i].ToString()); // Common case
        }
        
        return ApiSettings.GetFullUrl(url);
    }
}
