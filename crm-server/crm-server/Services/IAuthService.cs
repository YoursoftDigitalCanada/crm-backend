using crm_server.Entity;
using crm_server.Model;

namespace crm_server.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserDto request);
        Task<TokenResponseDto?> LoginAsync(UserDto request);
        Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request);
        Task<bool> SoftDeleteAsync(SoftDeleteDto request);
        Task<bool> RestoreUserAsync(Guid id);
        Task<bool> LogoutUserAsync(string RefreshToken);

    }
}
