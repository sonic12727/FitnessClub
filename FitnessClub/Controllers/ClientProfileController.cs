using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/client")]
    [Authorize(Roles = "Client")]
    public class ClientProfileController : ControllerBase
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<ClientProfileController> _logger;

        public ClientProfileController(FitnessClubDbContext context,ILogger<ClientProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Некорректный токен");
                }

                var user = await _context.Users.Include(u => u.Attendances).FirstOrDefaultAsync(u => u.Id == userId && u.Role == UserRole.Client);

                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var totalVisits = user.Attendances.Count;
                var lastMonthVisits = user.Attendances.Count(a => a.CheckInTime >= monthStart);

                return Ok(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Phone,
                    user.CreatedAt,
                    user.LastLoginAt,
                    TotalVisits = totalVisits,
                    LastMonthVisits = lastMonthVisits
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении профиля клиента");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpGet("membership")]
        public async Task<IActionResult> GetMembership()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Некорректный токен");
                }

                var user = await _context.Users
                    .Include(u => u.Membership)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.Role == UserRole.Client);

                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                if (user.Membership == null)
                {
                    return Ok(new
                    {
                        HasMembership = false,
                        Membership = (object?)null
                    });
                }

                return Ok(new
                {
                    HasMembership = true,
                    Membership = new
                    {
                        user.Membership.Type,
                        user.Membership.StartDate,
                        user.Membership.EndDate,
                        user.Membership.Price,
                        user.Membership.IsActive,
                        user.Membership.RemainingVisits,
                        IsValid = user.Membership.IsValid()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении абонемента клиента");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }
}