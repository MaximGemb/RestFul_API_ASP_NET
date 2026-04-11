using System.ComponentModel.DataAnnotations;

namespace RestFulApi.Models;

/// <summary>
/// Представляет событие, хранимое в системе.
/// </summary>
public class Event : IValidatableObject
{
    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    [Required]
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public Guid Id { get; set; }

    /// <summary>
    /// Название события.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = $"Поле {nameof(Title)} обязательно для заполнения.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание события.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    [Required(ErrorMessage = $"Поле {nameof(StartAt)} обязательно для заполнения.")]
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Дата и время завершения события.
    /// </summary>
    [Required(ErrorMessage = $"Поле {nameof(EndAt)} обязательно для заполнения.")]
    public DateTime? EndAt { get; set; }

    /// <summary>
    /// Выполняет пользовательскую валидацию дат события.
    /// </summary>
    /// <param name="validationContext">Контекст валидации текущего объекта.</param>
    /// <returns>Последовательность ошибок валидации, если они обнаружены.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt >= EndAt)
            yield return new ValidationResult(
                "Дата завершения должна быть позже даты начала.",
                [nameof(StartAt), nameof(EndAt)]);
    }
}