using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WebOtchet.Infrastructure;
using WebOtchet.Data;
using WebOtchet.Models;
using WebOtchet.Repositories.IRepository;
using WebOtchet.Repositories.Projections;

namespace WebOtchet.Repositories.Repository;

public sealed class UserRepository : IUserRepository
{
    private readonly WebotchetDbContext _context;

    public UserRepository(WebotchetDbContext context)
    {
        _context = context;
    }
public async Task<UserListItem?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new UserListItem(
            x.Id,
            x.Username,
            x.Role
        ))
        .FirstOrDefaultAsync(cancellationToken);
}

public async Task<User?> GetByIdWithHashAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken
        );
}
    public async Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        username = username.Trim();

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Username == username,
                cancellationToken
            );
    }

    public async Task<IReadOnlyList<UserListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .Select(x => new UserListItem(
                x.Id,
                x.Username,
                x.Role
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(
        string username,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        username = username.Trim();

        var query = _context.Users
            .Where(x => x.Username == username);

        if (excludeUserId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludeUserId.Value
            );
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();

        user.Username = user.Username.Trim();

        await _context.Users.AddAsync(
            user,
            cancellationToken
        );

        await SaveUserChangesAsync(cancellationToken);
    }

    private async Task SaveUserChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 })
        {
            throw new WriteConflictException("Этот логин уже используется.");
        }
    }

    // Lock the current administrators and the target before testing the invariant.
    private async Task<User?> LockUserForRoleChangeAsync(Guid id, Roles? newRole, CancellationToken cancellationToken)
    {
        var userId = id.ToString("D");
        var adminRole = (byte)Roles.Admin;
        var users = await _context.Users.FromSqlInterpolated(
            $"SELECT * FROM USERS WHERE ROLE = {adminRole} OR ID = {userId} ORDER BY ID FOR UPDATE")
            .ToListAsync(cancellationToken);
        var user = users.Find(item => item.Id == id);
        if (user?.Role == Roles.Admin && newRole != Roles.Admin && users.Count(item => item.Role == Roles.Admin) <= 1)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Нельзя удалить или понизить последнего администратора.");

        return user;
    }

    public async Task<bool> UpdateDetailsAsync(
        Guid id,
        string username,
        Roles role,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserForRoleChangeAsync(id, role, cancellationToken);

        if (user is null)
            return false;

        user.Username = username.Trim();
        user.Role = role;

        await SaveUserChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UpdatePasswordHashAsync(
        Guid id,
        string hash,
        CancellationToken cancellationToken = default,
        string? expectedHash = null)
    {
        var userId = id.ToString("D");
        var updated = expectedHash is null
            ? await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE USERS SET PASSWORD_HASH = {hash} WHERE ID = {userId}", cancellationToken)
            : await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE USERS SET PASSWORD_HASH = {hash} WHERE ID = {userId} AND BINARY PASSWORD_HASH = BINARY {expectedHash}",
                cancellationToken);
        return updated == 1;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserForRoleChangeAsync(id, null, cancellationToken);

        if (user is null)
            return false;

        _context.Users.Remove(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1451 })
        {
            throw new UserHasIncidentsException(exception);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
