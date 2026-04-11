using System.ComponentModel.DataAnnotations;

namespace RestFulApi.DTOs;

/// <summary>
/// DTO для передачи данных события через API.
/// </summary>
/// <param name="Title">Название события.</param>
/// <param name="Description">Описание события.</param>
/// <param name="StartAt">Дата и время начала события.</param>
/// <param name="EndAt">Дата и время завершения события.</param>
/// <param name="TotalSeats">Общее количество мест для события.</param>
public record EventDto(
    string Title,
    string? Description,
    DateTime? StartAt,
    DateTime? EndAt,
    int? TotalSeats
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
                [nameof(StartAt), nameof(EndAt)]);

        if (TotalSeats is null or <= 0)
            yield return new ValidationResult(
                "Общее количество мест должно быть больше нуля.",
                [nameof(TotalSeats)]);
    }

}