using MedRemind.Services.Authentication;
using Xunit;

namespace MedRemind.Tests.Services;

public class SecureStorageServiceTests
{
    private readonly SecureStorageService _secureStorage;

    public SecureStorageServiceTests()
    {
        _secureStorage = new SecureStorageService();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValue()
    {
        // Arrange
        var key = "test_key";
        var value = "test_value";

        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var key = "non_existent_key";

        // Act
        var result = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_MultipleValues_ShouldStoreIndependently()
    {
        // Arrange
        var key1 = "key1";
        var value1 = "value1";
        var key2 = "key2";
        var value2 = "value2";

        // Act
        await _secureStorage.SetAsync(key1, value1);
        await _secureStorage.SetAsync(key2, value2);

        var retrieved1 = await _secureStorage.GetAsync(key1);
        var retrieved2 = await _secureStorage.GetAsync(key2);

        // Assert
        Assert.Equal(value1, retrieved1);
        Assert.Equal(value2, retrieved2);
    }

    [Fact]
    public async Task SetAsync_OverwriteExisting_ShouldUpdateValue()
    {
        // Arrange
        var key = "update_key";
        var originalValue = "original";
        var newValue = "updated";

        // Act
        await _secureStorage.SetAsync(key, originalValue);
        await _secureStorage.SetAsync(key, newValue);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(newValue, retrieved);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteValue()
    {
        // Arrange
        var key = "remove_key";
        var value = "remove_value";
        await _secureStorage.SetAsync(key, value);

        // Act
        await _secureStorage.RemoveAsync(key);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_ShouldNotThrow()
    {
        // Arrange
        var key = "non_existent_remove_key";

        // Act & Assert - Should not throw
        await _secureStorage.RemoveAsync(key);
    }

    [Fact]
    public async Task ClearAllAsync_ShouldRemoveAllValues()
    {
        // Arrange
        await _secureStorage.SetAsync("key1", "value1");
        await _secureStorage.SetAsync("key2", "value2");
        await _secureStorage.SetAsync("key3", "value3");

        // Act
        await _secureStorage.ClearAllAsync();

        var value1 = await _secureStorage.GetAsync("key1");
        var value2 = await _secureStorage.GetAsync("key2");
        var value3 = await _secureStorage.GetAsync("key3");

        // Assert
        Assert.Null(value1);
        Assert.Null(value2);
        Assert.Null(value3);
    }

    [Fact]
    public async Task SetAsync_WithEmptyString_ShouldStore()
    {
        // Arrange
        var key = "empty_key";
        var value = "";

        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SetAsync_WithLongString_ShouldStore()
    {
        // Arrange
        var key = "long_key";
        var value = new string('A', 10000); // 10KB string

        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Theory]
    [InlineData("session_token", "abc123xyz")]
    [InlineData("user_id", "12345")]
    [InlineData("api_key", "sk-proj-abcdefg")]
    [InlineData("refresh_token", "rt-xyz-987")]
    public async Task SetAndGetAsync_WithCommonKeys_ShouldWork(string key, string value)
    {
        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SetAsync_WithSpecialCharacters_ShouldStore()
    {
        // Arrange
        var key = "special_key";
        var value = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SetAsync_WithUnicode_ShouldStore()
    {
        // Arrange
        var key = "unicode_key";
        var value = "Hello ?? ?? ?????";

        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SetAsync_WithJson_ShouldStore()
    {
        // Arrange
        var key = "json_key";
        var value = "{\"name\":\"Test\",\"age\":30,\"active\":true}";

        // Act
        await _secureStorage.SetAsync(key, value);
        var retrieved = await _secureStorage.GetAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await _secureStorage.SetAsync($"key{index}", $"value{index}");
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        for (int i = 0; i < 10; i++)
        {
            var value = await _secureStorage.GetAsync($"key{i}");
            Assert.Equal($"value{i}", value);
        }
    }

    [Fact]
    public async Task RemoveAsync_AfterClearAll_ShouldNotThrow()
    {
        // Arrange
        var key = "test_key";
        await _secureStorage.SetAsync(key, "value");
        await _secureStorage.ClearAllAsync();

        // Act & Assert - Should not throw
        await _secureStorage.RemoveAsync(key);
    }

    [Fact]
    public async Task GetAsync_AfterClearAll_ShouldReturnNull()
    {
        // Arrange
        await _secureStorage.SetAsync("key1", "value1");
        await _secureStorage.ClearAllAsync();

        // Act
        var result = await _secureStorage.GetAsync("key1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_AfterClearAll_ShouldWork()
    {
        // Arrange
        await _secureStorage.SetAsync("key1", "value1");
        await _secureStorage.ClearAllAsync();

        // Act
        await _secureStorage.SetAsync("key2", "value2");
        var result = await _secureStorage.GetAsync("key2");

        // Assert
        Assert.Equal("value2", result);
    }

    [Fact]
    public async Task MultipleOperations_ShouldMaintainIntegrity()
    {
        // Act
        await _secureStorage.SetAsync("k1", "v1");
        await _secureStorage.SetAsync("k2", "v2");
        await _secureStorage.RemoveAsync("k1");
        await _secureStorage.SetAsync("k3", "v3");
        await _secureStorage.SetAsync("k2", "v2_updated");

        // Assert
        Assert.Null(await _secureStorage.GetAsync("k1"));
        Assert.Equal("v2_updated", await _secureStorage.GetAsync("k2"));
        Assert.Equal("v3", await _secureStorage.GetAsync("k3"));
    }
}
