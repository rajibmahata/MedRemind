using MedRemind.Core.Interfaces;

namespace MedRemind.Services.Authentication;

public class SecureStorageService : ISecureStorageService
{
    // This will be implemented in the MAUI app layer using platform-specific SecureStorage
    // For now, this is an interface implementation that will be overridden

    private readonly Dictionary<string, string> _storage = new();

    public Task SetAsync(string key, string value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync()
    {
        _storage.Clear();
        return Task.CompletedTask;
    }
}
