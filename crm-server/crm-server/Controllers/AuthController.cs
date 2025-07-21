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
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            var token = await authService.LoginAsync(request);
            if(token == null)
            {
                return BadRequest("Incorrect Username or Password");
            }
            return Ok(token);
        }

        // add header when sending req - Authorization(key) : (value) Bearer jwt 
        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("You are authenticated thus only you can read this");
        }

        // for admins only route
        [Authorize(Roles ="Admin" )]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("You are an admin thus only you can read this");
        }
    }
}
