using Microsoft.AspNetCore.Mvc;
using TasksWebApi.Services;

namespace TasksWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody]UserCredentialsRequest userParam)
        {
            var user = _userService.Authenticate(userParam.Username, userParam.Password);
            
            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            return Ok(user);
        }
    }
}