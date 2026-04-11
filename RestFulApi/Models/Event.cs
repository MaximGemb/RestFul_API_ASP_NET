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
    /// Общее количество мест на событии.
    /// </summary>
    [Required(ErrorMessage = $"Поле {nameof(TotalSeats)} обязательно для заполнения.")]
    public int TotalSeats { get; init; }

    /// <summary>
    /// Текущее Количество свободных мест.
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Пытается зарезервировать указанное количество мест.
    /// </summary>
    /// <param name="count">Количество мест для резервирования (по умолчанию 1).</param>
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Освобождает указанное количество мест.
    /// </summary>
    /// <param name="count">Количество мест для освобождения (по умолчанию 1).</param>
    public void ReleaseSeats(int count = 1) =>
        AvailableSeats += count;

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

        if (TotalSeats <= 0)
            yield return new ValidationResult(
                "Общее количество мест должно быть больше нуля.",
                [nameof(TotalSeats)]);
    }
}