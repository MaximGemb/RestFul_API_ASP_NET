using Microsoft.AspNetCore.Mvc;

namespace RestFulApi.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при ошибках валидации входных данных.
/// Содержит словарь ошибок по полям.
/// </summary>
internal sealed class CustomValidationException : Exception
{
    /// <summary>
    /// Словарь ошибок валидации: ключ — название поля, значение — коллекция сообщений об ошибках.
    /// </summary>
    internal IDictionary<string, ICollection<string>> Errors { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения со словарём ошибок по нескольким полям.
    /// </summary>
    /// <param name="errors">Словарь ошибок: ключ — название поля, значение — коллекция сообщений.</param>
    internal CustomValidationException(IDictionary<string, ICollection<string>> errors) : base("Validation failed") => 
        Errors = errors;

    /// <summary>
    /// Инициализирует новый экземпляр исключения с одной ошибкой по указанному полю.
    /// </summary>
    /// <param name="field">Название поля, не прошедшего валидацию.</param>
    /// <param name="error">Сообщение об ошибке.</param>
    internal CustomValidationException(string field, string error) : base("Validation failed") =>
        Errors = new Dictionary<string, ICollection<string>>
        {
            { field, [error] }
        };
    
}