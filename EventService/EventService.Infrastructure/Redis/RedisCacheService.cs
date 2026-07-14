using EventService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventService.Infrastructure.Redis;

/// <summary>
/// Реализация <see cref="ICacheService"/> на основе Redis (StackExchange.Redis).
/// <para>
/// Любые ошибки взаимодействия с Redis (недоступность сервера, таймауты и т.д.)
/// перехватываются, логируются и не пробрасываются вызывающему коду — кеш деградирует
/// без ошибки для клиента, а запрос может быть обслужен напрямую из базы данных.
/// </para>
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="RedisCacheService"/>.
    /// </summary>
    /// <param name="connectionMultiplexer">Подключение к Redis.</param>
    /// <param name="logger">Логгер.</param>
    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis недоступен при получении значения по ключу {Key}. Кеш пропущен.", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis недоступен при записи значения по ключу {Key}. Запись пропущена.", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis недоступен при удалении значения по ключу {Key}. Удаление пропущено.", key);
        }
    }

    /// <summary>
    /// Определяет, относится ли исключение к сбою взаимодействия с Redis
    /// (недоступность сервера, таймаут, обрыв соединения), а не к ошибке программирования.
    /// </summary>
    private static bool IsRedisFailure(Exception ex) =>
        ex is RedisConnectionException or RedisTimeoutException or RedisServerException;
}
