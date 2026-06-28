using System.ComponentModel.DataAnnotations;

namespace EventService.Application.Options;

/// <summary>
/// Параметры JWT-токена, считываемые из конфигурации (секция "Jwt").
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Секретный ключ подписи (минимум 32 символа).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Издатель токена (iss).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Аудитория токена (aud).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Время жизни токена в минутах.
    /// </summary>
    [Range(15, 60)]
    public int ExpiryMinutes { get; init; } = 15;
}
