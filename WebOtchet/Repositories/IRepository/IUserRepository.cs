using WebOtchet.Models;
using WebOtchet.Repositories.Projections;

namespace WebOtchet.Repositories.IRepository;

public interface IUserRepository
{
    Task<UserListItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default
    );

    Task<User?> GetByIdWithHashAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<UserListItem>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<bool> UsernameExistsAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default
    );

    Task<bool> UpdateDetailsAsync(
        Guid id,
        string username,
        Roles role,
        CancellationToken cancellationToken = default
    );

    Task<bool> UpdatePasswordHashAsync(
        Guid id,
        string hash,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
    Task<int> CountByRoleAsync(
    Roles role,
    CancellationToken cancellationToken = default
);
}
