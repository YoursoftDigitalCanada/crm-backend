using crm_server.Entity;
using crm_server.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace crm_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IConfiguration  config) : ControllerBase
    {
        //static user
        public static User user = new();

        //Register route to create hashedpswd and return user
        [HttpPost("register")]
        public ActionResult<User> Register(UserDto request)
        {
            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user , request.Password);

            user.Username = request.Username;
            user.PasswordHash = hashedPassword;

            return Ok(user);
        }

        //Routing for login only check hashdpswd and username to return success or errmsg
        [HttpPost("login")]
        public ActionResult<string> Login(UserDto request)
        {
            if(user.Username != request.Username)
            {
                return BadRequest("User not found");
            }
            if(new PasswordHasher<User>().VerifyHashedPassword(user , user.PasswordHash , request.Password)
                == PasswordVerificationResult.Failed)
            {
                return BadRequest("wrong password");
            }

            //now only two attributes are there in class if both pass i.e doesnt return anything
            string token = CreateToken(user);
            return Ok(token);
        }

        //create a jwt taken with username as claims

        private string CreateToken(User user)
        {
            //used for providing claims in jwt token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name , user.Username)
            };

            //Unique key to show that it is from this server
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config.GetValue<string>("AppSetting:Token")!));

            //to define the security of jwt
            var cred = new SigningCredentials(key , SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: config.GetValue<string>("AppSetting:Issuer"),
                audience: config.GetValue<string>("AppSetting:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(2),
                signingCredentials : cred
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
