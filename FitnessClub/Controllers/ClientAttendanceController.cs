using FitnessClub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/client/attendance")]
    [Authorize(Roles = "Client")]
    public class ClientAttendanceController : ControllerBase
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<ClientAttendanceController> _logger;

        public ClientAttendanceController(
            FitnessClubDbContext context,
            ILogger<ClientAttendanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAttendances()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Некорректный токен");
                }

                var visits = await _context.Attendances
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CheckInTime)
                    .ToListAsync();

                return Ok(visits.Select(v => new
                {
                    v.Id,
                    v.CheckInTime,
                    v.CheckedByAdmin,
                    v.Notes
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении истории посещений клиента");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }
}