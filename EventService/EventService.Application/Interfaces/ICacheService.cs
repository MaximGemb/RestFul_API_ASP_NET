namespace EventService.Application.Interfaces;

/// <summary>
/// Абстракция кеша для слоя Application, изолирующая его от конкретной библиотеки кеширования (например, Redis).
/// <para>
/// Реализация должна деградировать без ошибок: если хранилище кеша недоступно,
/// ошибка логируется на уровне Infrastructure, а вызывающему коду возвращается
/// значение по умолчанию (для чтения) либо операция просто не выполняется (для записи/удаления),
/// чтобы запрос мог быть обслужен напрямую из базы данных.
/// </para>
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Возвращает значение по ключу или <c>null</c>, если значение отсутствует
    /// либо кеш временно недоступен.
    /// </summary>
    /// <param name="key">Ключ кеша.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Записывает значение по ключу с заданным временем жизни.
    /// При недоступности кеша операция молча не выполняется.
    /// </summary>
    /// <param name="key">Ключ кеша.</param>
    /// <param name="value">Значение для сохранения.</param>
    /// <param name="expiry">Время жизни записи.</param>
    /// <param name="ct">Токен отмены.</param>
    Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>
    /// Удаляет значение по ключу.
    /// При недоступности кеша операция молча не выполняется.
    /// </summary>
    /// <param name="key">Ключ кеша.</param>
    /// <param name="ct">Токен отмены.</param>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
