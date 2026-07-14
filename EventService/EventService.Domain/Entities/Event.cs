using EventService.Domain.Exceptions;

namespace EventService.Domain.Entities;

/// <summary>
/// Представляет событие, хранимое в системе.
/// Навигационные свойства к бронированиям удалены — связь только по идентификаторам.
/// </summary>
public sealed class Event
{
    /// <summary>
    /// Конструктор по умолчанию для Entity Framework.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    internal Event()
    {
        Title = null!;
    }

    /// <summary>
    /// Инициализирует новый экземпляр события с заданными параметрами.
    /// </summary>
    private Event(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        string? description = null)
    {
        Id = id;
        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        Description = description;
    }

    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public Guid Id { get; private set; }

    /// <summary>
    /// Название события.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Описание события.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Description { get; private set; }

    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    public DateTime? StartAt { get; private set; }

    /// <summary>
    /// Дата и время завершения события.
    /// </summary>
    public DateTime? EndAt { get; private set; }

    /// <summary>
    /// Общее количество мест на событии.
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Текущее количество свободных мест.
    /// </summary>
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// Создает новое событие с валидацией входных данных.
    /// </summary>
    /// <param name="title">Название события.</param>
    /// <param name="startAt">Дата и время начала.</param>
    /// <param name="endAt">Дата и время окончания.</param>
    /// <param name="totalSeats">Общее количество мест.</param>
    /// <param name="description">Описание события.</param>
    /// <param name="location">Место проведения события.</param>
    /// <returns>Новый экземпляр <see cref="Event"/>.</returns>
    /// <exception cref="CustomValidationException">Выбрасывается, если входные данные не прошли валидацию.</exception>
    public static Event Create(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats = null,
        string? description = null,
        string? location = null)
    {
        ThrowIfNotValid(title, startAt, endAt, totalSeats);

        return new Event(Guid.NewGuid(), title!.Trim(), startAt!.Value, endAt!.Value, totalSeats!.Value, description);
    }

    /// <summary>
    /// Обновляет данные события с валидацией входных данных.
    /// </summary>
    /// <param name="title">Новое название события.</param>
    /// <param name="startAt">Новая дата и время начала.</param>
    /// <param name="endAt">Новая дата и время окончания.</param>
    /// <param name="description">Новое описание события.</param>
    /// <param name="location">Новое место проведения события.</param>
    /// <exception cref="CustomValidationException">Выбрасывается, если входные данные не прошли валидацию.</exception>
    public void Update(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        string? description = null,
        string? location = null)
    {
        ThrowIfNotValid(title, startAt, endAt, TotalSeats);

        Title = title!;
        StartAt = startAt!.Value;
        EndAt = endAt!.Value;
        Description = description;
    }

    /// <summary>
    /// Пытается зарезервировать указанное количество мест.
    /// </summary>
    /// <param name="count">Количество мест для резервирования (по умолчанию 1).</param>
    public void TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            throw new NoAvailableSeatsException();

        AvailableSeats -= count;
    }

    /// <summary>
    /// Валидирует входные параметры события и выбрасывает исключение в случае ошибки.
    /// </summary>
    private static void ThrowIfNotValid(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats)
    {
        var errors = new Dictionary<string, ICollection<string>>();

        if (string.IsNullOrWhiteSpace(title))
            AddError(errors, nameof(Title), "Title cannot be empty");

        if (!startAt.HasValue)
            AddError(errors, nameof(StartAt), "Start time cannot be null");
        else if (startAt.Value < DateTime.UtcNow)
            AddError(errors, nameof(StartAt), "Event cannot start in the past");

        if (!endAt.HasValue)
            AddError(errors, nameof(EndAt), "End time cannot be null");

        if (endAt <= startAt)
            AddError(errors, nameof(EndAt), "End time must be after start time");

        if (totalSeats is not > 0)
            AddError(errors, nameof(TotalSeats), "TotalSeats must be greater than zero");

        if (errors.Count is not 0)
            throw new CustomValidationException(errors);
    }

    private static void AddError(Dictionary<string, ICollection<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();

        errors[field].Add(message);
    }
}
