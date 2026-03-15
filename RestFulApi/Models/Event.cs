using System.ComponentModel.DataAnnotations;

namespace RestFulApi.Models;

/// <summary>
/// Модель для хранения данных
/// </summary>
public class Event : IValidatableObject
{
    /// <summary>
    /// Идентификатор события
    /// </summary>
    [Required]
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public Guid Id { get; set; }

    /// <summary>
    /// Название события
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = $"Поле {nameof(Title)} обязательно для заполнения.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание события
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    [Required(ErrorMessage = $"Поле {nameof(StartAt)} обязательно для заполнения.")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [Required(ErrorMessage = $"Поле {nameof(EndAt)} обязательно для заполнения.")]
    public DateTime EndAt { get; set; }

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