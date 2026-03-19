using FitnessClub.Core.Entities;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessClub.Core.Services
{
    public class AttendanceService
    {
        private readonly FitnessClubDbContext _context;

        public AttendanceService(FitnessClubDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanUserCheckIn(int userId)
        {
            var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Membership == null)
            {
                return false;
            }

            if (!user.Membership.IsValid())
            {
                return false;
            }

            // Проверка, не заходил ли уже сегодня
            var today = DateTime.Today;
            var hasCheckedInToday = await _context.Attendances.AnyAsync(a => a.UserId == userId && a.CheckInTime.Date == today);
            return !hasCheckedInToday;
        }

        // Автоматический вход
        public async Task<Attendance?> AutoCheckIn(int userId)
        {
            if (!await CanUserCheckIn(userId))
                return null;

            var attendance = new Attendance
            {
                UserId = userId,
                CheckInTime = DateTime.Now,
                CheckedByAdmin = "Автоматическая система",
                Notes = "Автоматический вход"
            };

            // Логика входа для разовых абонементов
            var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Membership?.Type == Core.Enums.MembershipType.OneTime)
            {
                user.Membership.RemainingVisits--;
                if (user.Membership.RemainingVisits <= 0)
                {
                    user.Membership.IsActive = false;
                }
            }

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return attendance;
        }

        // Метод получения статистики за период
        public async Task<List<Attendance>> GetAttendanceReport(DateTime from, DateTime to)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .ThenInclude(u => u.Membership)
                .Where(a => a.CheckInTime >= from && a.CheckInTime <= to)
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();
        }

        // Метод получения статистики по дням
        public async Task<Dictionary<DateTime, int>> GetDailyStats(int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days);

            var stats = await _context.Attendances
                .Where(a => a.CheckInTime >= startDate)
                .GroupBy(a => a.CheckInTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            return stats;
        }
    }
}
