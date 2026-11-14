using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using WebOtchet.Models;
using WebOtchet.Repositories.IRepository;
using WebOtchet.Repositories.Projections;
using WebOtchet.Services.Helpers;

namespace WebOtchet.Services;

public sealed class UserService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher<User> _passwordHasher;

    private const int MinUsernameLength = 3;
    private const int MaxUsernameLength = 100;

    private const int MinPasswordLength = 12;
    private const int MaxPasswordLength = 128;

    public UserService(
        IUserRepository repository,
        IPasswordHasher<User> passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }


    public Task<IReadOnlyList<UserListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

public async Task<UserListItem?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    ValidateId(id);

    return await _repository.GetByIdAsync(
        id,
        cancellationToken
    );
}

    public async Task<UserListItem> CreateAsync(
        string username,
        string password,
        Roles role,
        CancellationToken cancellationToken = default)
    {
        username = ValidateUsername(username);

        ValidatePassword(password);

        ValidateRole(role);

        if (await _repository.UsernameExistsAsync(
                username,
                cancellationToken: cancellationToken))
        {
            throw new ValidationException(
                "Пользователь с таким логином уже существует."
            );
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Role = role
        };

        user.Hash = _passwordHasher.HashPassword(
            user,
            password
        );

        await _repository.AddAsync(
            user,
            cancellationToken
        );


return new UserListItem(
    user.Id,
    user.Username,
    user.Role
);}


    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            return null;
        }

        username = username.Trim();

        var user = await _repository.GetByUsernameAsync(
            username,
            cancellationToken
        );

        if (user is null)
            return null;

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.Hash,
                password
            );

        if (verificationResult == PasswordVerificationResult.Failed)
            return null;

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newHash = _passwordHasher.HashPassword(
                user,
                password
            );

            await _repository.UpdatePasswordHashAsync(
                user.Id,
                newHash,
                cancellationToken
            );
        }

        return new AuthenticatedUser(
            user.Id,
            user.Username,
            user.Role
        );
    }


public async Task<bool> UpdateDetailsAsync(
    Guid id,
    string username,
    Roles role,
    CancellationToken cancellationToken = default)
{
    ValidateId(id);

    username = ValidateUsername(username);

    ValidateRole(role);

    var existingUser = await _repository.GetByIdAsync(
        id,
        cancellationToken
    );

    if (existingUser is null)
        return false;

    if (await _repository.UsernameExistsAsync(
            username,
            id,
            cancellationToken))
    {
        throw new ValidationException(
            "Этот логин уже используется другим пользователем."
        );
    }

    await EnsureAdministratorRemainsAsync(
        existingUser,
        role,
        cancellationToken
    );

    return await _repository.UpdateDetailsAsync(
        id,
        username,
        role,
        cancellationToken
    );
}

    public async Task<bool> ChangePasswordAsync(
        Guid id,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        ValidatePassword(newPassword);

        if (string.IsNullOrEmpty(currentPassword))
        {
            throw new ValidationException(
                "Не указан текущий пароль."
            );
        }

var user = await _repository.GetByIdWithHashAsync(
    id,
    cancellationToken
);
        if (user is null)
            return false;

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.Hash,
            currentPassword
        );

        if (verification == PasswordVerificationResult.Failed)
        {
            throw new ValidationException(
                "Текущий пароль указан неверно."
            );
        }

        var samePassword = _passwordHasher.VerifyHashedPassword(
            user,
            user.Hash,
            newPassword
        );

        if (samePassword != PasswordVerificationResult.Failed)
        {
            throw new ValidationException(
                "Новый пароль должен отличаться от текущего."
            );
        }

        var newHash = _passwordHasher.HashPassword(
            user,
            newPassword
        );

        return await _repository.UpdatePasswordHashAsync(
            id,
            newHash,
            cancellationToken
        );
    }


    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var user = await _repository.GetByIdAsync(
            id,
            cancellationToken
        );

        if (user is null)
            return false;

        await EnsureAdministratorRemainsAsync(
            user,
            null,
            cancellationToken
        );

        return await _repository.DeleteAsync(
            id,
            cancellationToken
        );
    }


    private static string ValidateUsername(string username)
    {
        username = (username ?? "").Trim();

        if (username.Length < MinUsernameLength)
        {
            throw new ValidationException(
                $"Логин должен содержать минимум {MinUsernameLength} символа."
            );
        }

        if (username.Length > MaxUsernameLength)
        {
            throw new ValidationException(
                $"Логин не должен превышать {MaxUsernameLength} символов."
            );
        }

        if (username.Any(char.IsControl))
        {
            throw new ValidationException(
                "Логин содержит недопустимые символы."
            );
        }

        return username;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ValidationException(
                "Пароль не может быть пустым."
            );
        }

        if (password.Length < MinPasswordLength)
        {
            throw new ValidationException(
                $"Пароль должен содержать минимум {MinPasswordLength} символов."
            );
        }

        if (password.Length > MaxPasswordLength)
        {
            throw new ValidationException(
                $"Пароль не должен превышать {MaxPasswordLength} символов."
            );
        }
    }

    private static void ValidateRole(Roles role)
    {
        if (!Enum.IsDefined(typeof(Roles), role))
        {
            throw new ValidationException(
                "Указана неизвестная роль пользователя."
            );
        }
    }

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException(
                "Некорректный идентификатор пользователя."
            );
        }
    }


private async Task EnsureAdministratorRemainsAsync(
    UserListItem existingUser,
    Roles? newRole,
    CancellationToken cancellationToken)
{
    if (existingUser.Role != Roles.Admin)
        return;

    if (newRole == Roles.Admin)
        return;

    int administratorCount =
        await _repository.CountByRoleAsync(
            Roles.Admin,
            cancellationToken
        );

    if (administratorCount <= 1)
    {
        throw new ValidationException(
            "Нельзя удалить или понизить последнего администратора."
        );
    }
}
}
