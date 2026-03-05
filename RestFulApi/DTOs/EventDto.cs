using System.ComponentModel.DataAnnotations;

namespace RestFulApi.DTOs;

/// <summary>
/// DTO для работы с API
/// </summary>
/// <param name="Title">Название</param>
/// <param name="Description">Описание</param>
/// <param name="StartAt">Дата начала события</param>
/// <param name="EndAt">Дата завершения события</param>
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
    /// Валидация. Дата начала должна быть меньше даты окончания
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt >= EndAt)
            yield return new ValidationResult(
                "Дата завершения должна быть позже даты начала.",
                [nameof(StartAt), nameof(EndAt)]);
    }
}