using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FitnessClub.Core.Requests;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/admin/attendance")]
    [Authorize(Roles = "Admin")]
    public class AdminAttendanceController : ControllerBase
    {
        private readonly AttendanceService _attendanceService;
        private readonly ILogger<AdminAttendanceController> _logger;

        public AdminAttendanceController(AttendanceService attendanceService,ILogger<AdminAttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
        {
            try
            {
                var adminName = User.FindFirst(ClaimTypes.Email)?.Value ?? "Администратор";
                var attendance = await _attendanceService.MarkAttendanceByAdminAsync(request.UserId, adminName);

                return Ok(new
                {
                    attendance.Id,
                    attendance.CheckInTime,
                    attendance.CheckedByAdmin,
                    Message = "Посещение отмечено"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отметке посещения пользователя {UserId}", request.UserId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("today-visits")]
        public async Task<IActionResult> GetTodayVisits([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date?.Date ?? DateTime.UtcNow.Date;
                var visits = await _attendanceService.GetVisitsByDateAsync(targetDate);
                return Ok(visits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении посещений за день");
                return StatusCode(500, new { message = "Не удалось получить посещения" });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetAttendanceHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            try
            {
                var visits = await _attendanceService.GetAttendanceHistoryAsync(from, to);
                return Ok(visits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении истории посещений");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("today-stats")]
        public async Task<IActionResult> GetTodayAttendanceStats()
        {
            try
            {
                var stats = await _attendanceService.GetTodayAttendanceStatsAsync(DateTime.UtcNow.Date);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики посещений за день");
                return BadRequest(ex.Message);
            }
        }
    }
}
