namespace EventService.Application.Common;

/// <summary>
/// Централизованные ключи кеша, используемые для событий.
/// Общие для слоя Application и Infrastructure, чтобы избежать рассинхронизации форматов ключей.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Ключ кеша для события по идентификатору: <c>event:{id}</c>.
    /// </summary>
    public static string Event(Guid id) => $"event:{id}";

    /// <summary>
    /// Ключ кеша для топ-10 самых популярных событий: <c>events:top10</c>.
    /// </summary>
    public const string TopEvents = "events:top10";
}
