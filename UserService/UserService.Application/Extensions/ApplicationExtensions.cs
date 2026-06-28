using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces;
using UserService.Application.Services;

namespace UserService.Application.Extensions;

/// <summary>
/// Регистрация зависимостей слоя Application.
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Регистрирует сервисы слоя Application.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService.Application.Services.UserService>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        return services;
    }
}
