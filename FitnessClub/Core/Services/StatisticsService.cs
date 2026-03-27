using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessClub.Core.Services
{
    public class StatisticsService
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(FitnessClubDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<object> GetStatisticsAsync()
        {
            var now = DateTime.UtcNow;

            var totalClients = await _context.Users
                .CountAsync(u => u.Role == UserRole.Client);

            var activeMemberships = await _context.Memberships
                .CountAsync(m => m.IsActive && m.EndDate > now);

            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var todayVisits = await _context.Attendances
                .CountAsync(a => a.CheckInTime >= todayStart && a.CheckInTime < todayEnd);

            var totalRevenue = await _context.Memberships
                .SumAsync(m => (decimal?)m.Price) ?? 0;

            return new
            {
                TotalClients = totalClients,
                ActiveMemberships = activeMemberships,
                TodayVisits = todayVisits,
                TotalRevenue = totalRevenue
            };
        }

        public async Task<object> GetDetailedStatisticsAsync(DateTime from, DateTime to)
        {
            if (from > to)
            {
                throw new Exception("Дата 'от' не может быть позже даты 'до'");
            }

            var start = from.Date;
            var end = to.Date.AddDays(1);

            _logger.LogInformation("Запрос статистики с {From} по {To}", start, end);

            var totalRevenue = await _context.Memberships
                .Where(m => m.StartDate >= start && m.StartDate < end)
                .SumAsync(m => (decimal?)m.Price) ?? 0;

            var totalVisits = await _context.Attendances
                .CountAsync(a => a.CheckInTime >= start && a.CheckInTime < end);

            var daysCount = (to.Date - from.Date).Days + 1;
            var avgDailyVisits = daysCount > 0
                ? Math.Round((double)totalVisits / daysCount, 1)
                : 0;

            var activeClients = await _context.Memberships
                .CountAsync(m => m.IsActive && m.EndDate > DateTime.UtcNow);

            var attendanceGrouped = await _context.Attendances
                .Where(a => a.CheckInTime >= start && a.CheckInTime < end)
                .GroupBy(a => a.CheckInTime.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var attendanceLabels = new List<string>();
            var attendanceData = new List<int>();

            for (var date = start; date < end; date = date.AddDays(1))
            {
                var item = attendanceGrouped.FirstOrDefault(x => x.Date == date);
                attendanceLabels.Add(date.ToString("dd.MM"));
                attendanceData.Add(item?.Count ?? 0);
            }

            var membershipTypes = await _context.Memberships
                .Where(m => m.IsActive)
                .GroupBy(m => m.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var membershipLabels = membershipTypes.Select(m =>
                m.Type.ToString() switch
                {
                    "OneTime" => "Разовые",
                    "Monthly" => "Месячные",
                    "Quarterly" => "Квартальные",
                    "Yearly" => "Годовые",
                    _ => m.Type.ToString()
                }).ToList();

            var membershipData = membershipTypes.Select(m => m.Count).ToList();

            if (!membershipLabels.Any())
            {
                membershipLabels = new List<string> { "Нет данных" };
                membershipData = new List<int> { 1 };
            }

            var revenueGrouped = await _context.Memberships
                .Where(m => m.StartDate >= start && m.StartDate < end)
                .GroupBy(m => new { m.StartDate.Year, m.StartDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.Price)
                })
                .ToListAsync();

            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();

            var startMonth = new DateTime(start.Year, start.Month, 1);
            var endMonth = new DateTime(to.Year, to.Month, 1);

            for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
            {
                var item = revenueGrouped.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                revenueLabels.Add(month.ToString("MMM yyyy"));
                revenueData.Add(item?.Revenue ?? 0);
            }

            var clientsLabels = new List<string>();
            var newClientsData = new List<int>();
            var activeClientsData = new List<int>();

            var currentDate = start;
            while (currentDate <= to.Date)
            {
                var weekEnd = currentDate.AddDays(6);
                if (weekEnd > to.Date) weekEnd = to.Date;

                var weekEndExclusive = weekEnd.AddDays(1);

                var newCount = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Client &&
                                     u.CreatedAt >= currentDate &&
                                     u.CreatedAt < weekEndExclusive);

                var activeCount = await _context.Attendances
                    .Where(a => a.CheckInTime >= currentDate &&
                                a.CheckInTime < weekEndExclusive)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                clientsLabels.Add($"{currentDate:dd.MM}-{weekEnd:dd.MM}");
                newClientsData.Add(newCount);
                activeClientsData.Add(activeCount);

                currentDate = currentDate.AddDays(7);
            }

            return new
            {
                TotalRevenue = totalRevenue,
                TotalVisits = totalVisits,
                AvgDailyVisits = avgDailyVisits,
                ActiveClients = activeClients,

                AttendanceLabels = attendanceLabels,
                AttendanceData = attendanceData,

                MembershipLabels = membershipLabels,
                MembershipData = membershipData,

                RevenueLabels = revenueLabels,
                RevenueData = revenueData,

                ClientsLabels = clientsLabels,
                NewClientsData = newClientsData,
                ActiveClientsData = activeClientsData,

                PeriodInfo = new
                {
                    From = start.ToString("yyyy-MM-dd"),
                    To = to.Date.ToString("yyyy-MM-dd"),
                    DaysCount = daysCount
                }
            };
        }
    }
}
