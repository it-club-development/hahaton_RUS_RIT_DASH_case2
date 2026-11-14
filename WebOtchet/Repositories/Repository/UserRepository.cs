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

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateDetailsAsync(
        Guid id,
        string username,
        Roles role,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

        if (user is null)
            return false;

        user.Username = username.Trim();
        user.Role = role;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UpdatePasswordHashAsync(
        Guid id,
        string hash,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

        if (user is null)
            return false;

        user.Hash = hash;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

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

        return true;
    }
public async Task<int> CountByRoleAsync(
    Roles role,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .CountAsync(
            x => x.Role == role,
            cancellationToken
        );
}
}
