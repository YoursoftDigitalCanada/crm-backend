using crm_server.Data;
using crm_server.Entity;
using crm_server.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public async Task<string?> LoginAsync(UserDto request)
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

            //if user and pswd match return jwt
            return CreateToken(user);
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
