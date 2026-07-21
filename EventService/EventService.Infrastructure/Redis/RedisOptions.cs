using System.Diagnostics.CodeAnalysis;

namespace EventService.Infrastructure.Redis;

/// <summary>
/// Параметры подключения к Redis.
/// Считываются из секции <c>Redis</c> конфигурации приложения.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class RedisOptions
{
    /// <summary>Адрес (host:port) узла Redis.</summary>
    public required string EndPoint { get; set; }

    /// <summary>Пароль для аутентификации на сервере Redis.</summary>
    public required string Password { get; set; }

    /// <summary>Таймаут установления соединения, мс.</summary>
    public required int ConnectTimeout { get; set; }

    /// <summary>Таймаут синхронных операций, мс.</summary>
    public required int SyncTimeout { get; set; }

    /// <summary>
    /// Если true — при неудачном подключении соединение не будет прерывать работу приложения,
    /// а попытается переподключиться в фоне.
    /// </summary>
    public bool AbortOnConnectFail { get; set; }
}
