using FitnessClub.Core.Entities;
using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;
using FitnessClub.Core.Utils;

namespace FitnessClub.Core.Services
{
    public class AttendanceService
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(FitnessClubDbContext context, ILogger<AttendanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Attendance> MarkAttendanceByAdminAsync(int userId, string adminName)
        {
            var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            if (user.Role != UserRole.Client)
            {
                throw new Exception("Отмечать посещения можно только для клиента");
            }

            if (user.Membership == null || !user.Membership.IsValid())
            {
                throw new Exception("У клиента нет активного абонемента");
            }

            var nowUtc = DateTime.UtcNow;
            var localToday = ClubTimeHelper.GetLocalToday();
            var (startOfDayUtc, endOfDayUtc) = ClubTimeHelper.GetUtcBoundsForLocalDay(localToday);

            var alreadyVisitedToday = await _context.Attendances.AnyAsync(a=>a.UserId == userId && a.CheckInTime >= startOfDayUtc && a.CheckInTime < endOfDayUtc);

            if (alreadyVisitedToday)
            {
                throw new Exception("Клиент уже отмечен сегодня");
            }

            if (user.Membership.IsVisitBased())
            {
                if (user.Membership.RemainingVisits <= 0)
                {
                    throw new Exception("Посещения по абонементу закончились");
                }

                user.Membership.RemainingVisits--;

                if (user.Membership.RemainingVisits <= 0)
                {
                    user.Membership.IsActive = false;
                }
            }

            var attendance = new Attendance
            {
                UserId = userId,
                CheckInTime = nowUtc,
                CheckedByAdmin = adminName,
                Notes = "Посещение отмечено администратором"
            };

            await _context.Attendances.AddAsync(attendance);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Посещение отмечено для пользователя {UserId} администратором {AdminName}", userId, adminName);

            return attendance;
        }

        public async Task<List<object>> GetVisitsByDateAsync(DateTime date)
        {
            var localDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
            var localEnd = localDate.AddDays(1);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDate, TimeZoneInfo.Local);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, TimeZoneInfo.Local);

            var visits = await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();

            return visits.Select(v => (object)new
            {
                v.Id,
                v.CheckInTime,
                CheckedByAdmin = v.CheckedByAdmin ?? "Система",
                v.Notes,
                User = v.User == null ? null : new
                {
                    v.User.Id,
                    v.User.FirstName,
                    v.User.LastName,
                    v.User.Email
                }
            }).ToList();
        }

        public async Task<List<object>> GetAttendanceHistoryAsync(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1);

            var visits = await _context.Attendances
                .Include(a => a.User)
                .ThenInclude(u => u.Membership)
                .Where(a => a.CheckInTime >= start && a.CheckInTime < end)
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();

            return visits.Select(v => (object)new
            {
                v.Id,
                v.UserId,
                v.CheckInTime,
                v.CheckedByAdmin,
                v.Notes,
                User = v.User == null ? null : new
                {
                    v.User.Id,
                    v.User.FirstName,
                    v.User.LastName,
                    v.User.Email
                }
            }).ToList();
        }

        public async Task<object> GetTodayAttendanceStatsAsync(DateTime date)
        {
            var (startUtc, endUtc) = ClubTimeHelper.GetUtcBoundsForLocalDay(date);

            var totalVisits = await _context.Attendances.CountAsync(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc);

            var activeClients = await _context.Attendances
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            var firstVisit = await _context.Attendances
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .OrderBy(a => a.CheckInTime)
                .FirstOrDefaultAsync();

            var lastVisit = await _context.Attendances
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync();

            return new
            {
                TotalVisits = totalVisits,
                ActiveClients = activeClients,
                FirstVisitTime = firstVisit == null
                    ? null
                    : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(firstVisit.CheckInTime, DateTimeKind.Utc), ClubTimeHelper.ClubTimeZone).ToString("HH:mm"),
                LastVisitTime = lastVisit == null
                    ? null
                    : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(lastVisit.CheckInTime, DateTimeKind.Utc), ClubTimeHelper.ClubTimeZone).ToString("HH:mm")
            };
        }
    }
}
