using FitnessClub.Core.Enums;
using FitnessClub.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace FitnessClub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly FitnessClub.Data.FitnessClubDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        // Метод создания клиента в системе
        [HttpPost("create-client")]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
        {
            try
            {
                var client = await _adminService.CreateClient(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.Phone);

                return Ok(new
                {
                    client.Id,
                    client.FirstName,
                    client.LastName,
                    client.Email,
                    client.Phone,
                    HasMembership = client.Membership != null,
                    Message = "Клиент успешно создан"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Метод добавления абонемента клиенту
        [HttpPost("add-membership")]
        public async Task<IActionResult> AddMembership([FromBody] AddMembershipRequest request)
        {
            try
            {
                if (!Enum.TryParse<FitnessClub.Core.Enums.MembershipType>(request.Type, out var membershipType))
                {
                    return BadRequest("Некорректный тип абонемента. Доступные: OneTime, Monthly, Quarterly, Yearly");
                }

                var membership = await _adminService.AddMembership(
                    request.UserId,
                    membershipType,
                    request.DurationMonths,
                    request.Price);

                return Ok(new
                {
                    membership.Id,
                    membership.Type,
                    membership.StartDate,
                    membership.EndDate,
                    membership.Price,
                    membership.IsActive,
                    membership.RemainingVisits,
                    Message = "Абонемент успешно добавлен/обновлён"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Метод поиска клиента
        [HttpGet("search-clients")]
        public async Task<IActionResult> SearchClients([FromQuery] string search = "")
        {
            try
            {
                var clients = await _adminService.SearchClients(search);
                return Ok(clients.Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.Phone,
                    c.CreatedAt,
                    Membership = c.Membership == null ? null : new
                    {
                        c.Membership.Type,
                        c.Membership.StartDate,
                        c.Membership.EndDate,
                        c.Membership.IsActive,
                        c.Membership.RemainingVisits,
                        IsValid = c.Membership.IsValid()
                    }
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Метод отметки посещения клиента, через ЛК админа
        [HttpPost("mark-attendance")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
        {
            try
            {
                var adminName = User.FindFirst(ClaimTypes.Email)?.Value ?? "Администратор";
                var attendance = await _adminService.MarkAttendance(request.UserId, adminName);

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
                return BadRequest(ex.Message);
            }
        }

        // Добавить эти методы в AdminController:

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var statistics = await _adminService.GetStatistics();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("today-visits")]
        public async Task<IActionResult> GetTodayVisits([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = DateTime.Today;

                var visits = await _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.CheckInTime.Date == targetDate)
                    .ToListAsync();

                var result = visits.Select(v => new
                {
                    v.Id,
                    v.CheckInTime,
                    CheckedByAdmin = v.CheckedByAdmin ?? "Система",
                    v.Notes,
                    User = v.User != null ? new
                    {
                        v.User.Id,
                        v.User.FirstName,
                        v.User.LastName,
                        v.User.Email
                    } : null
                }).ToList();

                return Ok(result);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("client-details/{id}")]
        public async Task<IActionResult> GetClientDetails(int id)
        {
            try
            {
                var client = await _context.Users
                    .Include(u => u.Membership)
                    .Include(u => u.Attendances)
                    .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Client);

                if (client == null)
                {
                    _logger.LogWarning("Клиент с ID {ClientId} не найден", id);
                    return NotFound(new
                    {
                        Message = "Клиент не найден",
                        ClientId = id,
                        Status = "error"
                    });
                }

                return Ok(new
                {
                    client.Id,
                    client.FirstName,
                    client.LastName,
                    client.Email,
                    client.Phone,
                    client.CreatedAt,
                    Membership = client.Membership == null ? null : new
                    {
                        client.Membership.Type,
                        client.Membership.StartDate,
                        client.Membership.EndDate,
                        client.Membership.Price,
                        client.Membership.IsActive,
                        client.Membership.RemainingVisits,
                        IsValid = client.Membership.IsValid()
                    },
                    TotalVisits = client.Attendances.Count,
                    LastVisit = client.Attendances.OrderByDescending(a => a.CheckInTime).FirstOrDefault()?.CheckInTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("today-attendance-stats")]
        public async Task<IActionResult> GetTodayAttendanceStats()
        {
            try
            {
                var today = DateTime.Today;

                var totalVisits = await _context.Attendances
                    .CountAsync(a => a.CheckInTime.Date == today);

                var activeClients = await _context.Attendances
                    .Where(a => a.CheckInTime.Date == today)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                var firstVisit = await _context.Attendances
                    .Where(a => a.CheckInTime.Date == today)
                    .OrderBy(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                var lastVisit = await _context.Attendances
                    .Where(a => a.CheckInTime.Date == today)
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    TotalVisits = totalVisits,
                    ActiveClients = activeClients,
                    FirstVisitTime = firstVisit?.CheckInTime.ToString("HH:mm"),
                    LastVisitTime = lastVisit?.CheckInTime.ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("attendance-history")]
        public async Task<IActionResult> GetAttendanceHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            try
            {
                var visits = await _context.Attendances
                    .Include(a => a.User)
                    .ThenInclude(u => u.Membership)
                    .Where(a => a.CheckInTime.Date >= from.Date && a.CheckInTime.Date <= to.Date)
                    .OrderByDescending(a => a.CheckInTime)
                    .ToListAsync();

                return Ok(visits);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("statistics-detailed")]
        public async Task<IActionResult> GetDetailedStatistics([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            try
            {
                // Валидация дат
                if (from > to)
                {
                    return BadRequest(new { Error = "Дата 'от' не может быть позже даты 'до'" });
                }

                _logger.LogInformation("Запрос статистики с {From} по {To}", from, to);

                // 1. ОБЩАЯ СТАТИСТИКА (РЕАЛЬНЫЕ ДАННЫЕ)
                var totalRevenue = await _context.Memberships
                    .Where(m => m.StartDate >= from && m.StartDate <= to)
                    .SumAsync(m => (decimal?)m.Price) ?? 0;

                var totalVisits = await _context.Attendances
                    .CountAsync(a => a.CheckInTime.Date >= from.Date && a.CheckInTime.Date <= to.Date);

                var daysCount = (to - from).Days + 1;
                var avgDailyVisits = daysCount > 0 ? Math.Round((double)totalVisits / daysCount, 1) : 0;

                var activeClients = await _context.Memberships
                    .CountAsync(m => m.IsActive && m.EndDate > DateTime.Now);

                // 2. ПОСЕЩАЕМОСТЬ ПО ДНЯМ (РЕАЛЬНЫЕ ДАННЫЕ)
                var attendanceLabels = new List<string>();
                var attendanceData = new List<int>();

                for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
                {
                    var count = await _context.Attendances
                        .CountAsync(a => a.CheckInTime.Date == date);

                    attendanceLabels.Add(date.ToString("dd.MM"));
                    attendanceData.Add(count);
                }

                // 3. РАСПРЕДЕЛЕНИЕ АБОНЕМЕНТОВ (РЕАЛЬНЫЕ ДАННЫЕ)
                var membershipTypes = await _context.Memberships
                    .Where(m => m.IsActive)
                    .GroupBy(m => m.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                var membershipLabels = membershipTypes.Select(m =>
                    m.Type.ToString() switch
                    {
                        "OneTime" => "Разовые",
                        "Monthly" => "Месячные",
                        "Quarterly" => "Квартальные",
                        "Yearly" => "Годовые",
                        _ => m.Type.ToString()
                    }
                ).ToList();

                var membershipData = membershipTypes.Select(m => m.Count).ToList();

                // Если нет абонементов, показываем заглушку
                if (!membershipLabels.Any())
                {
                    membershipLabels = new List<string> { "Нет данных" };
                    membershipData = new List<int> { 1 };
                }

                // 4. ДОХОД ПО МЕСЯЦАМ (РЕАЛЬНЫЕ ДАННЫЕ)
                var revenueLabels = new List<string>();
                var revenueData = new List<decimal>();

                var startMonth = new DateTime(from.Year, from.Month, 1);
                var endMonth = new DateTime(to.Year, to.Month, 1);

                for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
                {
                    var monthRevenue = await _context.Memberships
                        .Where(m => m.StartDate.Year == month.Year && m.StartDate.Month == month.Month)
                        .SumAsync(m => (decimal?)m.Price) ?? 0;

                    revenueLabels.Add(month.ToString("MMM yyyy"));
                    revenueData.Add(monthRevenue);
                }

                // 5. АКТИВНОСТЬ КЛИЕНТОВ (РЕАЛЬНЫЕ ДАННЫЕ)
                var clientsLabels = new List<string>();
                var newClientsData = new List<int>();
                var activeClientsData = new List<int>();

                var currentDate = from;
                while (currentDate <= to)
                {
                    var weekEnd = currentDate.AddDays(6);
                    if (weekEnd > to) weekEnd = to;

                    // Новые клиенты за неделю
                    var newCount = await _context.Users
                        .CountAsync(u => u.Role == UserRole.Client &&
                                        u.CreatedAt.Date >= currentDate.Date &&
                                        u.CreatedAt.Date <= weekEnd.Date);

                    // Активные клиенты (с посещениями на этой неделе)
                    var activeCount = await _context.Attendances
                        .Where(a => a.CheckInTime.Date >= currentDate.Date &&
                                   a.CheckInTime.Date <= weekEnd.Date)
                        .Select(a => a.UserId)
                        .Distinct()
                        .CountAsync();

                    clientsLabels.Add($"{currentDate:dd.MM}-{weekEnd:dd.MM}");
                    newClientsData.Add(newCount);
                    activeClientsData.Add(activeCount);

                    currentDate = currentDate.AddDays(7);
                }

                // ВОЗВРАЩАЕМ ТОЛЬКО РЕАЛЬНЫЕ ДАННЫЕ
                return Ok(new
                {
                    // Общая статистика
                    TotalRevenue = totalRevenue,
                    TotalVisits = totalVisits,
                    AvgDailyVisits = avgDailyVisits,
                    ActiveClients = activeClients,

                    // Данные для графиков
                    AttendanceLabels = attendanceLabels,
                    AttendanceData = attendanceData,

                    MembershipLabels = membershipLabels,
                    MembershipData = membershipData,

                    RevenueLabels = revenueLabels,
                    RevenueData = revenueData,

                    ClientsLabels = clientsLabels,
                    NewClientsData = newClientsData,
                    ActiveClientsData = activeClientsData,

                    // Информация о периоде
                    PeriodInfo = new
                    {
                        From = from.ToString("yyyy-MM-dd"),
                        To = to.ToString("yyyy-MM-dd"),
                        DaysCount = daysCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики");
                return BadRequest(new { Error = "Не удалось загрузить статистику" });
            }
        }
    }
        public class CreateClientRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
    }

    public class AddMembershipRequest
    {
        public int UserId { get; set; }
        public string Type { get; set; }
        public int DurationMonths { get; set; }
        public decimal Price { get; set; }
    }

    public class MarkAttendanceRequest
    {
        public int UserId { get; set; }
    }
}
