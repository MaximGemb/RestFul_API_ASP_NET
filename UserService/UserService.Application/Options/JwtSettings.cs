using System.ComponentModel.DataAnnotations;

namespace UserService.Application.Options;

/// <summary>
/// Параметры JWT-токена, считываемые из конфигурации (секция "Jwt").
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Секретный ключ подписи (минимум 32 символа).
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT Secret не может быть пустым.")]
    [MinLength(32, ErrorMessage = "JWT Secret должен содержать не менее 32 символов.")]
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Издатель токена (iss).
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT Issuer должен быть указан.")]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Аудитория токена (aud).
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "JWT Audience должен быть указана.")]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Время жизни токена в минутах (не более 60 минут).
    /// </summary>
    [Range(15, 60, ErrorMessage = "Время жизни токена (ExpiryMinutes) должно быть в диапазоне от 15 до 60 минут.")]
    public int ExpiryMinutes { get; init; } = 15;
}
