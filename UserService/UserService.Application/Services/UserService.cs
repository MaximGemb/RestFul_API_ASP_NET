using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;

namespace UserService.Application.Services;

/// <summary>
/// Сервис для регистрации и аутентификации пользователей.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UserService"/>.
    /// </summary>
    /// <param name="userRepository">Репозиторий пользователей.</param>
    /// <param name="passwordHasher">Сервис хеширования паролей.</param>
    /// <param name="jwtTokenService">Сервис генерации JWT-токенов.</param>
    public UserService(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<Guid> RegisterAsync(
        string login,
        string password,
        Roles role = Roles.User,
        CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByLoginAsync(login, ct))
            throw new LoginAlreadyExistsException(login);

        var hash = _passwordHasher.Hash(password);
        var user = User.Create(login, hash, role);

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return user.Id;
    }

    /// <inheritdoc />
    public async Task<string> LoginAsync(string login, string password, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByLoginAsync(login, ct)
                   ?? throw new InvalidCredentialsException();

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return _jwtTokenService.GenerateToken(user.Id, user.Login, user.Role);
    }
}
