namespace RestFulApi.Models;

/// <summary>
/// Представляет задачу на генерацию отчета.
/// </summary>
public class BookingTask
{
    /// <summary>
    /// Уникальный идентификатор задачи.
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public Guid Id { get; set; }

    /// <summary>
    /// Тип отчета.
    /// </summary>
    public string BookingType { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания задачи.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
