using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccountService.Utility;
using AccountService.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace AccountService.Controllers
{
    [ApiController]
    [Route("v1")]

    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // POST /v1/user
        [HttpPost("user")]
        public async Task<IActionResult> Register([FromBody] User user){
            
            if (!ModelState.IsValid) { return BadRequest(ModelState); }

            if (await _userService.Register(user))
            {
                return Ok(new { message = "User registered successfully.", user });
            }

            return BadRequest(new { message = "Failed to register user. Please try again." });
        }
        
        
        // POST /v1/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel logindata){
            
            if (!ModelState.IsValid) { return BadRequest(ModelState); }
            

            var token = _userService.Login(logindata);
            if (token != null)
            {
                return Ok(new { message = "User logged-in successfully.", token });
            }

            return BadRequest(new { message = "Failed to log-in user. Please try again." });
        }
        
        

        // GET /v1/user/{user_id}
        [HttpGet("user/{user_id}")]
        [Authorize]
        public IActionResult GetUser(int user_id)
        {
            var UserData = _userService.GetUser(user_id);

            if(UserData != null)
            {
                return Ok(new { message = "User Found.", UserData });
            }
            else
            {
                return NotFound(new { message = "User Not Found." });
            }
        }

        // PUT /v1/user/{user_id}
        [HttpPut("user/{user_id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int user_id, [FromBody] User newData)
        {
            if(await _userService.UpdateUser(user_id, newData)){
                return Ok(new {message = "User Profile Upadated", newData});
            }
            else{

                return NotFound(new { message = "User Not Found." });
            }
        }

    }
}