using System.ComponentModel.DataAnnotations;

namespace RestFulApi.DTOs;

/// <summary>
/// DTO для передачи данных события через API.
/// </summary>
/// <param name="Title">Название события.</param>
/// <param name="Description">Описание события.</param>
/// <param name="StartAt">Дата и время начала события.</param>
/// <param name="EndAt">Дата и время завершения события.</param>
public record EventDto(
    [Required(ErrorMessage = $"Поле {nameof(Title)} обязательно для заполнения.")]
    string Title,
    string? Description,

    [Required(ErrorMessage = $"Поле {nameof(StartAt)} обязательно для заполнения.")]
    DateTime StartAt,
    [Required(ErrorMessage = $"Поле {nameof(EndAt)} обязательно для заполнения.")]
    DateTime EndAt
) : IValidatableObject
{
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
                new[] { nameof(StartAt), nameof(EndAt) });
    }
}