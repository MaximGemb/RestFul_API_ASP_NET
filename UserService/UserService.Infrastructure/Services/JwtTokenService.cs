using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UserService.Application.Interfaces;
using UserService.Application.Options;
using UserService.Domain.Entities;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace UserService.Infrastructure.Services;

/// <summary>
/// Генерирует подписанные JWT-токены на основе параметров из конфигурации.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    /// <summary>
    /// Инициализирует сервис с параметрами из секции "Jwt" конфигурации.
    /// </summary>
    /// <param name="options">Параметры JWT.</param>
    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    /// <inheritdoc />
    public string GenerateToken(Guid userId, string login, Roles role)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = $"{userId}",
            [JwtRegisteredClaimNames.UniqueName] = login,
            ["role"] = role.ToString(),
            [JwtRegisteredClaimNames.Jti] = $"{Guid.NewGuid()}"
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            SigningCredentials = creds
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
