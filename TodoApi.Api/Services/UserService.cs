using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Auth;
using TodoApi.Api.Data;
using TodoApi.Api.Dtos;
using TodoApi.Api.Models;

namespace TodoApi.Api.Services;

public sealed class UserService(TodoDbContext db, IPasswordHasher<User> hasher, ITokenService tokens) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetAllAsync(int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var totalCount = await db.Users.CountAsync(ct);
        var entities = await db.Users.AsNoTracking()
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<UserResponse>(entities.Select(UserResponse.FromEntity).ToList(), page, pageSize, totalCount);
    }

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : UserResponse.FromEntity(user);
    }

    public Task<UserResult> RegisterAsync(RegisterRequest request, CancellationToken ct) =>
        CreateUserAsync(request.Email, request.DisplayName, request.Password, UserRole.User, ct);

    public Task<UserResult> CreateAsync(CreateUserRequest request, CancellationToken ct) =>
        CreateUserAsync(request.Email, request.DisplayName, request.Password, request.Role, ct);

    public async Task<UserResult> UpdateAsync(int id, UpdateUserRequest request, CurrentUser actor, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return UserResult.Fail(UserError.NotFound);
        }

        // Guards against an admin locking themselves out; another admin can still change their role.
        if (actor.Id == id && request.Role != user.Role)
        {
            return UserResult.Fail(UserError.SelfModification);
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Role = request.Role;
        if (request.Password is not null)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return UserResult.Ok(UserResponse.FromEntity(user));
    }

    public async Task<UserError> DeleteAsync(int id, CurrentUser actor, CancellationToken ct)
    {
        if (actor.Id == id)
        {
            return UserError.SelfModification;
        }

        // ON DELETE CASCADE on Todos.OwnerId removes the user's todos in the same statement.
        var deleted = await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0 ? UserError.None : UserError.NotFound;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return null;
        }

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
        }

        var (accessToken, expiresAt) = tokens.CreateToken(user);
        return new LoginResponse(accessToken, expiresAt, UserResponse.FromEntity(user));
    }

    private async Task<UserResult> CreateUserAsync(string email, string displayName, string password, UserRole role, CancellationToken ct)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            return UserResult.Fail(UserError.EmailTaken);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = displayName.Trim(),
            PasswordHash = string.Empty,
            Role = role,
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.PasswordHash = hasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return UserResult.Ok(UserResponse.FromEntity(user));
    }
}
