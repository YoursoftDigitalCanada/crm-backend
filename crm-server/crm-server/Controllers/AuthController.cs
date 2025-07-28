using crm_server.Entity;
using crm_server.Model;
using crm_server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace crm_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {

        //Register route to create hashedpswd and return user
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            var user = await authService.RegisterAsync(request);
            if(user == null)
            {
                return BadRequest("Username already exist");
            }

            return Ok(user);
        }

        //Routing for login only check hashdpswd and username to return success or errmsg
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(UserDto request)
        {
            var result = await authService.LoginAsync(request);
            if(result == null)
            {
                return BadRequest("Incorrect Username or Password");
            }
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokensAsync(request);
            if (result == null || result.AccessToken is null || result.RefreshToken is null)
            {
                return Unauthorized("Invalid Refresh Token");
            }
            return Ok(result);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUser(SoftDeleteDto request)
        {
            var result = await authService.SoftDeleteAsync(request);

            if (!result)
                return NotFound("User not found or already deleted.");

            return Ok("User marked as deleted.");
        }


        // add header when sending req - Authorization(key) : (value) Bearer jwt 
        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("You are authenticated thus only you can read this");
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto request)
        {
            bool result = await authService.LogoutUserAsync(request.RefreshToken);
            

            if (!result)
            {
                return Unauthorized("Invalid refresh token");
            }

            return Ok(new { message = "Successfully logged out" });
        }


        // for admins only route
        [Authorize(Roles ="Admin" )]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("You are an admin thus only you can read this");
        }

        [Authorize(Roles ="Admin")]
        [HttpPost("restore-user")]
        public async Task<IActionResult> RestoreUserEndpointAsync([FromBody] RestoreUserDto request)
        {
            bool result = await authService.RestoreUserAsync(request.Id);
            if (!result)
                return NotFound("User not found or not deleted.");

            return Ok("User restored.");
        }
    }
}
