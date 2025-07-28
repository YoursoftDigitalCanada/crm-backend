using Azure.Core;
using crm_server.Data;
using crm_server.Entity;
using crm_server.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace crm_server.Services
{
    public class AuthService(UserDbContext context, IConfiguration configuration) : IAuthService
    {
        public async Task<User?> RegisterAsync(UserDto request)
        {
            //check if user already exist
            if(await context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
            {
                return null;
            }

            //create user and pswd
            var user = new User();
            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, request.Password);

            user.Username = request.Username;
            user.PasswordHash = hashedPassword;

            //store user in database and save the changes 
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<TokenResponseDto?> LoginAsync(UserDto request)
        {
            // get user from db and check if user doesnt exist or pswd doesnt match return null
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
            if (user is null)
            {
                return null;
            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return await CreateTokenResponse(user);
        }

        public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
            {
                return null;
            }
            return await CreateTokenResponse(user);
        }

        public async Task<bool> SoftDeleteAsync(SoftDeleteDto request)
        {
            var user = await context.Users.FindAsync(request.UserId);
            if (user == null || user.IsDeleted)
                return false;

            string? performedBy = GetRoleFromToken(request.AccessToken);

            if(performedBy is null )
            {
                return false;
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = performedBy;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreUserAsync(Guid id)
        {
            var user = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null || !user.IsDeleted)
                return false;

            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletedBy = null;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LogoutUserAsync(string RefreshToken)
        {
            //find user in db
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.RefreshTokens == RefreshToken);

            if (user == null)
            {
                return false;
            }

            // Invalidate the refresh token
            user.RefreshTokens = null;
            user.RefreshTokenExpiryTime = null;

            await context.SaveChangesAsync();
            return true;
        }


        private string? GetRoleFromToken(string jwtToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Validate token format
            if (!tokenHandler.CanReadToken(jwtToken))
                return null;

            var token = tokenHandler.ReadJwtToken(jwtToken);

            return token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        }


        private async Task<TokenResponseDto> CreateTokenResponse(User user)
        {
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }

        private async Task<User?> ValidateRefreshTokenAsync(Guid UserId, string RefreshToken)
        {
            var user = await context.Users.FindAsync(UserId);
            if (user is null || user.RefreshTokens != RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            return user;
        }
        //method to create rfrsh_tkn
        private string GenerateRefreshToken()
        {
            var randomNumber = new Byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        //addrfrsh_tkn and expiry in db
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshTokens = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;
        }
        private string CreateToken(User user)
        {
            //used for providing claims in jwt token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name , user.Username),
                new Claim(ClaimTypes.NameIdentifier , user.Id.ToString()),
                new Claim(ClaimTypes.Role , user.Role),
            };

            //Unique key to show that it is from this server
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSetting:Token")!));

            //to define the security of jwt
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSetting:Issuer"),
                audience: configuration.GetValue<string>("AppSetting:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(2),
                signingCredentials: cred
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        
    }
}
