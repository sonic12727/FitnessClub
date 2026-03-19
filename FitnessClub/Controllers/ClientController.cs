using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly AttendanceService _attendanceService;
        private readonly FitnessClub.Data.FitnessClubDbContext _context;

        public ClientController( AttendanceService attendanceService, FitnessClub.Data.FitnessClubDbContext context)
        {
            _attendanceService = attendanceService;
            _context = context;
        }

        // Метод получения информации о своём абонементе
        [HttpGet("my-membership")]
        public async Task<IActionResult> GetMyMembership()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var user = await _context.Users.Include(u => u.Membership).Include(u => u.Attendances) .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }   

            var lastMonthAttendances = user.Attendances.Where(a => a.CheckInTime >= DateTime.Now.AddMonths(-1)).Count();

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                Membership = user.Membership == null ? null : new
                {
                    user.Membership.Type,
                    user.Membership.StartDate,
                    user.Membership.EndDate,
                    user.Membership.Price,
                    user.Membership.IsActive,
                    user.Membership.RemainingVisits,
                    IsValid = user.Membership.IsValid()
                },
                LastMonthVisits = lastMonthAttendances,
                TotalVisits = user.Attendances.Count
            });
        }

        // Метод проверки возможости входа
        [HttpGet("can-check-in")]
        public async Task<IActionResult> CanCheckIn()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var canCheckIn = await _attendanceService.CanUserCheckIn(userId);

            return Ok(new { CanCheckIn = canCheckIn });
        }

        // Получить мои посещения
        [HttpGet("my-attendances")]
        public async Task<IActionResult> GetMyAttendances()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var attendances = await _context.Attendances.Where(a => a.UserId == userId).OrderByDescending(a => a.CheckInTime).Take(20).ToListAsync();

            return Ok(attendances);
        }
    }
}