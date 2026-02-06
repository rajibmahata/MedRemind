using System.Security.Cryptography;
using System.Text;

namespace MedRemind.Core.Services;

/// <summary>
/// Service for secure password hashing and verification using PBKDF2
/// </summary>
public class PasswordHashingService
{
    private const int SaltSize = 32; // 256 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000; // OWASP recommended minimum

    /// <summary>
    /// Hash a password using PBKDF2 with a random salt
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentNullException(nameof(password));
        }

        // Generate a random salt
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        // Hash the password with PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        var hash = pbkdf2.GetBytes(HashSize);

        // Combine salt and hash
        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        // Convert to base64 for storage
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verify a password against a stored hash
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        try
        {
            // Extract the bytes from the stored hash
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract the salt
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Hash the provided password with the extracted salt
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            var hash = pbkdf2.GetBytes(HashSize);

            // Compare the computed hash with the stored hash
            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generate a secure random password reset token
    /// </summary>
    public string GeneratePasswordResetToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_");
    }

    /// <summary>
    /// Validate password strength
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        if (password.Length > 128)
        {
            return (false, "Password must not exceed 128 characters");
        }

        // Check for at least one uppercase letter
        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter");
        }

        // Check for at least one lowercase letter
        if (!password.Any(char.IsLower))
        {
            return (false, "Password must contain at least one lowercase letter");
        }

        // Check for at least one digit
        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one number");
        }

        // Check for at least one special character
        var specialCharacters = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        if (!password.Any(c => specialCharacters.Contains(c)))
        {
            return (false, "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)");
        }

        return (true, null);
    }
}
