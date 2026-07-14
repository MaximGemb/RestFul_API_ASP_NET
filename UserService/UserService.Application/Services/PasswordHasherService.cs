using System.Security.Cryptography;
using System.Text;
using UserService.Application.Interfaces;

namespace UserService.Application.Services;

/// <summary>
/// Хеширование паролей с использованием SHA-256.
/// </summary>
public sealed class PasswordHasherService : IPasswordHasherService
{
    /// <inheritdoc />
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public bool Verify(string password, string hash)
    {
        return Hash(password) == hash;
    }
}
