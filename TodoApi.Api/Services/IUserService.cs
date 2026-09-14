using TodoApi.Api.Auth;
using TodoApi.Api.Dtos;

namespace TodoApi.Api.Services;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetAllAsync(int page, int pageSize, CancellationToken ct);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserResult> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<UserResult> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserResult> UpdateAsync(int id, UpdateUserRequest request, CurrentUser actor, CancellationToken ct);
    Task<UserError> DeleteAsync(int id, CurrentUser actor, CancellationToken ct);
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}
