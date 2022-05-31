using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using WebApi.Code;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly UsersService _usersService;

        public LoginController(ILogger<LoginController> logger, UsersService usersService)
        {
            _logger = logger;
            _usersService = usersService;
        }

        [AllowAnonymous]
        [HttpPost("[action]")]
        public async Task<string> Login(LoginDto loginDto)
        {
            var token = await _usersService.LoginAsync(loginDto.UserName, loginDto.Password);

            return token;
        }
    }
}