using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using FitnessClub.Core.Utils;

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

        // Вспомогательные record-типы
        private sealed record AttendancePreviewRow(string Date, string Client, string Email, string MarkedBy);
        private sealed record FinancialPreviewRow(string Date, string Client, string MembershipType, decimal Amount);
        private sealed record ClientsPreviewRow(string Client, string Phone, string Email, int VisitsCount, string LastVisit);
        public sealed record ExcelExportResult(byte[] Content, string FileName);

        // Предпросмотр и экспорт Excel
        public async Task<object> GetReportPreviewAsync(string type, DateTime from, DateTime to)
        {
            type = (type ?? string.Empty).Trim().ToLowerInvariant();

            return type switch
            {
                "attendance" => new
                {
                    Type = "attendance",
                    Title = "Предпросмотр отчета по посещениям",
                    Rows = await GetAttendancePreviewRowsAsync(from, to)
                },
                "financial" => new
                {
                    Type = "financial",
                    Title = "Предпросмотр финансового отчета",
                    Rows = await GetFinancialPreviewRowsAsync(from, to)
                },
                "clients" => new
                {
                    Type = "clients",
                    Title = "Предпросмотр отчета по клиентам",
                    Rows = await GetClientsPreviewRowsAsync(from, to)
                },
                _ => throw new Exception("Неизвестный тип отчета")
            };
        }

        public async Task<ExcelExportResult> ExportExcelAsync(string type, DateTime from, DateTime to)
        {
            type = (type ?? string.Empty).Trim().ToLowerInvariant();

            using var workbook = new XLWorkbook();

            switch (type)
            {
                case "summary":
                    {
                        var detailed = await GetDetailedStatisticsAsync(from, to);

                        var summarySheet = workbook.Worksheets.Add("Общая статистика");
                        summarySheet.Cell(1, 1).Value = "Показатель";
                        summarySheet.Cell(1, 2).Value = "Значение";

                        dynamic data = detailed;

                        summarySheet.Cell(2, 1).Value = "Общий доход";
                        summarySheet.Cell(2, 2).Value = data.TotalRevenue;

                        summarySheet.Cell(3, 1).Value = "Всего посещений";
                        summarySheet.Cell(3, 2).Value = data.TotalVisits;

                        summarySheet.Cell(4, 1).Value = "Среднее в день";
                        summarySheet.Cell(4, 2).Value = data.AvgDailyVisits;

                        summarySheet.Cell(5, 1).Value = "Активных клиентов";
                        summarySheet.Cell(5, 2).Value = data.ActiveClients;

                        var topClientsSheet = workbook.Worksheets.Add("Топ клиентов");
                        topClientsSheet.Cell(1, 1).Value = "#";
                        topClientsSheet.Cell(1, 2).Value = "Клиент";
                        topClientsSheet.Cell(1, 3).Value = "Посещений";

                        var topClients = await GetTopActiveClientsRowsAsync(from, to);

                        for (int i = 0; i < topClients.Count; i++)
                        {
                            topClientsSheet.Cell(i + 2, 1).Value = i + 1;
                            topClientsSheet.Cell(i + 2, 2).Value = topClients[i].Client;
                            topClientsSheet.Cell(i + 2, 3).Value = topClients[i].VisitsCount;
                        }
                        break;
                    }

                case "attendance":
                    {
                        var sheet = workbook.Worksheets.Add("Посещения");
                        sheet.Cell(1, 1).Value = "Дата";
                        sheet.Cell(1, 2).Value = "Клиент";
                        sheet.Cell(1, 3).Value = "Email";
                        sheet.Cell(1, 4).Value = "Отметил";

                        var rows = await GetAttendancePreviewRowsAsync(from, to);
                        for (int i = 0; i < rows.Count; i++)
                        {
                            sheet.Cell(i + 2, 1).Value = rows[i].Date;
                            sheet.Cell(i + 2, 2).Value = rows[i].Client;
                            sheet.Cell(i + 2, 3).Value = rows[i].Email;
                            sheet.Cell(i + 2, 4).Value = rows[i].MarkedBy;
                        }

                        break;
                    }

                case "financial":
                    {
                        var sheet = workbook.Worksheets.Add("Финансы");
                        sheet.Cell(1, 1).Value = "Дата";
                        sheet.Cell(1, 2).Value = "Клиент";
                        sheet.Cell(1, 3).Value = "Тип абонемента";
                        sheet.Cell(1, 4).Value = "Сумма";

                        var rows = await GetFinancialPreviewRowsAsync(from, to);

                        for (int i = 0; i < rows.Count; i++)
                        {
                            sheet.Cell(i + 2, 1).Value = rows[i].Date;
                            sheet.Cell(i + 2, 2).Value = rows[i].Client;
                            sheet.Cell(i + 2, 3).Value = rows[i].MembershipType;
                            sheet.Cell(i + 2, 4).Value = rows[i].Amount;
                        }

                        break;
                    }

                case "clients":
                    {
                        var sheet = workbook.Worksheets.Add("Клиенты");
                        sheet.Cell(1, 1).Value = "Клиент";
                        sheet.Cell(1, 2).Value = "Телефон";
                        sheet.Cell(1, 3).Value = "Email";
                        sheet.Cell(1, 4).Value = "Посещений";
                        sheet.Cell(1, 5).Value = "Последнее посещение";

                        var rows = await GetClientsPreviewRowsAsync(from, to);

                        for (int i = 0; i < rows.Count; i++)
                        {
                            sheet.Cell(i + 2, 1).Value = rows[i].Client;
                            sheet.Cell(i + 2, 2).Value = rows[i].Phone;
                            sheet.Cell(i + 2, 3).Value = rows[i].Email;
                            sheet.Cell(i + 2, 4).Value = rows[i].VisitsCount;
                            sheet.Cell(i + 2, 5).Value = rows[i].LastVisit;
                        }
                        break;
                    }

                default:
                    throw new Exception("Неизвестный тип Excel-экспорта");
            }

            foreach (var ws in workbook.Worksheets)
            {
                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"statistics-{type}-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx";
            return new ExcelExportResult(stream.ToArray(), fileName);
        }

        // Вспомогательные методы для предпросмотра и экспорта
        private (DateTime StartUtc, DateTime EndUtc) BuildUtcRange(DateTime from, DateTime to)
        {
            var localStart = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
            var localEndExclusive = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Unspecified);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.Local);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEndExclusive, TimeZoneInfo.Local);

            return (startUtc, endUtc);
        }

        private async Task<List<AttendancePreviewRow>> GetAttendancePreviewRowsAsync(DateTime from, DateTime to)
        {
            var (startUtc, endUtc) = BuildUtcRange(from, to);

            return await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .OrderByDescending(a => a.CheckInTime)
                .Select(a => new AttendancePreviewRow(a.CheckInTime.ToString("dd.MM.yyyy HH:mm"),
                    ((a.User.FirstName ?? "") + " " + (a.User.LastName ?? "")).Trim(),
                    a.User.Email ?? "—",
                    a.CheckedByAdmin ?? "Система"
                ))
                .ToListAsync();
        }

        private async Task<List<FinancialPreviewRow>> GetFinancialPreviewRowsAsync(DateTime from, DateTime to)
        {
            var (startUtc, endUtc) = BuildUtcRange(from, to);

            return await _context.Memberships
                .Include(m => m.User)
                .Where(m => m.StartDate >= startUtc && m.StartDate < endUtc)
                .OrderByDescending(m => m.StartDate)
                .Select(m => new FinancialPreviewRow(m.StartDate.ToString("dd.MM.yyyy"),
                    ((m.User.FirstName ?? "") + " " + (m.User.LastName ?? "")).Trim(),
                    m.Type.ToString(),
                    m.Price
                ))
                .ToListAsync();
        }

        private async Task<List<ClientsPreviewRow>> GetClientsPreviewRowsAsync(DateTime from, DateTime to)
        {
            var (startUtc, endUtc) = BuildUtcRange(from, to);

            var rows = await _context.Attendances
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    VisitsCount = g.Count(),
                    LastVisit = g.Max(x => x.CheckInTime)
                })
                .OrderByDescending(x => x.VisitsCount)
                .Join(
                    _context.Users,
                    g => g.UserId,
                    u => u.Id,
                    (g, u) => new ClientsPreviewRow(
                        ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                        u.Phone ?? "—",
                        u.Email ?? "—",
                        g.VisitsCount,
                        g.LastVisit.ToString("dd.MM.yyyy HH:mm")
                    ))
                .ToListAsync();

            return rows;
        }

        private async Task<List<ClientsPreviewRow>> GetTopActiveClientsRowsAsync(DateTime from, DateTime to)
        {
            return await GetClientsPreviewRowsAsync(from, to);
        }

        public async Task<object> GetStatisticsAsync()
        {
            var nowUtc = DateTime.UtcNow;
            var localToday = ClubTimeHelper.GetLocalToday();
            var (todayStartUtc, todayEndUtc) = ClubTimeHelper.GetUtcBoundsForLocalDay(localToday);

            var totalClients = await _context.Users.CountAsync(u => u.Role == UserRole.Client);

            var activeMemberships = await _context.Memberships.CountAsync(m => m.IsActive && m.EndDate > nowUtc);

            var todayVisits = await _context.Attendances.CountAsync(a => a.CheckInTime >= todayStartUtc && a.CheckInTime < todayEndUtc);

            var totalRevenue = await _context.Memberships.SumAsync(m => (decimal?)m.Price) ?? 0;

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

            var localStart = from.Date;
            var localEnd = to.Date;
            var (startUtc, endUtc) = ClubTimeHelper.GetUtcBoundsForLocalRange(localStart, localEnd);

            _logger.LogInformation("Запрос статистики с {FromLocal} по {ToLocal}", localStart, localEnd);

            var totalRevenue = await _context.Memberships.Where(m => m.StartDate >= startUtc && m.StartDate < endUtc).SumAsync(m => (decimal?)m.Price) ?? 0;
            var totalVisits = await _context.Attendances.CountAsync(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc);

            var daysCount = (to.Date - from.Date).Days + 1;
            var avgDailyVisits = daysCount > 0 ? Math.Round((double)totalVisits / daysCount, 1) : 0;

            var activeClients = await _context.Attendances
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            var attendances = await _context.Attendances.Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc).ToListAsync();

            var attendanceGrouped = attendances
                .GroupBy(a => TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(a.CheckInTime, DateTimeKind.Utc),
                    TimeZoneInfo.Local).Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var attendanceLabels = new List<string>();
            var attendanceData = new List<int>();

            for (var date = localStart; date <= localEnd; date = date.AddDays(1))
            {
                attendanceLabels.Add(date.ToString("dd.MM"));
                attendanceData.Add(attendanceGrouped.TryGetValue(date, out var count) ? count : 0);
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
                    "Visits8" => "8 посещений",
                    "Visits12" => "12 посещений",
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

            var membershipsInRange = await _context.Memberships.Where(m => m.StartDate >= startUtc && m.StartDate < endUtc).ToListAsync();

            var revenueGrouped = membershipsInRange
                .GroupBy(m =>
                {
                    var localDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(m.StartDate, DateTimeKind.Utc),TimeZoneInfo.Local);

                    return new { localDate.Year, localDate.Month };
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.Price)
                })
                .ToList();

            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();

            var startMonth = new DateTime(localStart.Year, localStart.Month, 1);
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

            var currentDate = localStart.Date;

            while (currentDate <= to.Date)
            {
                var weekEnd = currentDate.AddDays(6);
                if (weekEnd > to.Date) weekEnd = to.Date;

                var weekStartLocal = DateTime.SpecifyKind(currentDate, DateTimeKind.Unspecified);
                var weekEndExclusiveLocal = DateTime.SpecifyKind(weekEnd.AddDays(1), DateTimeKind.Unspecified);

                var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartLocal, TimeZoneInfo.Local);
                var weekEndUtc = TimeZoneInfo.ConvertTimeToUtc(weekEndExclusiveLocal, TimeZoneInfo.Local);

                var newCount = await _context.Users.CountAsync(u => u.Role == UserRole.Client && u.CreatedAt >= weekStartUtc && u.CreatedAt < weekEndUtc);

                var activeCount = await _context.Attendances
                    .Where(a => a.CheckInTime >= weekStartUtc && a.CheckInTime < weekEndUtc)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                clientsLabels.Add($"{currentDate:dd.MM}-{weekEnd:dd.MM}");
                newClientsData.Add(newCount);
                activeClientsData.Add(activeCount);

                currentDate = currentDate.AddDays(7);
            }

            var topActiveClients = await _context.Attendances
                .Where(a => a.CheckInTime >= startUtc && a.CheckInTime < endUtc)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    VisitsCount = g.Count()
                })
                .OrderByDescending(x => x.VisitsCount)
                .Take(5)
                .Join(
                    _context.Users,
                    g => g.UserId,
                    u => u.Id,
                    (g, u) => new
                    {
                        FullName = (u.FirstName + " " + u.LastName).Trim(),
                        VisitsCount = g.VisitsCount
                    })
                .ToListAsync();

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

                TopActiveClients = topActiveClients,

                PeriodInfo = new
                {
                    From = localStart.ToString("yyyy-MM-dd"),
                    To = to.Date.ToString("yyyy-MM-dd"),
                    DaysCount = daysCount
                }
            };
        }
    }
}
