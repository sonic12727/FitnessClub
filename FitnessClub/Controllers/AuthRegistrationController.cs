using FitnessClub.Core.Requests;
using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthRegistrationController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthRegistrationController> _logger;

        public AuthRegistrationController(AuthService authService,ILogger<AuthRegistrationController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            try
            {
                var user = await _authService.Register(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.Phone);

                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя {Email}", request.Email);
                return BadRequest(ex.Message);
            }
        }
    }
}
