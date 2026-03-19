using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Mvc;


namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly JwtService _jwtService;


        public AuthController(AuthService authService, JwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Email и пароль обязательны");
                }

                var user = await _authService.Login(request.Email, request.Password);
                if (user == null)
                {
                    return Unauthorized("Неверный email или пароль");
                }

                var token = _jwtService.GenerateToken(user);

                var response = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    MembershipValid = user.Membership?.IsValid(), // Проверка абонемента
                    Token = token
                };
                return new ObjectResult(response)
                {
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _authService.Register(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.Phone,
                    request.Role);

                var responce = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.CreatedAt
                };
                return Ok(responce);

            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
    }
}
