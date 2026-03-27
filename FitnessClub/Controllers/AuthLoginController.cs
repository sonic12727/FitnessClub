using FitnessClub.Core.Requests;
using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthLoginController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthLoginController> _logger;

        public AuthLoginController(AuthService authService,JwtService jwtService,ILogger<AuthLoginController> logger)
        {
            _authService = authService;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _authService.Login(request.Email, request.Password);
                if (user == null)
                {
                    return Unauthorized("Неверный email или пароль");
                }

                var token = _jwtService.GenerateToken(user);

                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    MembershipValid = user.Membership?.IsValid(),
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя {Email}", request.Email);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }
}
