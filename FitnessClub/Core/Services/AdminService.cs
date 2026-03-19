using FitnessClub.Core.Entities;
using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace FitnessClub.Core.Services
{
    public class AdminService
    {
        private readonly FitnessClubDbContext _context;
        private readonly AuthService _authService;

        public AdminService(FitnessClubDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // Метод создания клиента 
        public async Task<User> CreateClient(string email, string password,
            string firstName, string lastName, string phone)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                throw new Exception("Пользователь с таким email уже существует");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                Role = UserRole.Client,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return await _context.Users .Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == user.Id);

        }

        // Метод добавления абонемента клиенту
        public async Task<Membership> AddMembership(int userId, MembershipType type, int durationMonths, decimal price)
        {
            var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            if (user.Membership == null)
            {
                // Новый абонемент
                user.Membership = new Membership
                {
                    UserId = userId,
                    Type = type,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(durationMonths),
                    Price = price,
                    IsActive = true,
                    RemainingVisits = type == MembershipType.OneTime ? 1 : 0
                };
                _context.Memberships.Add(user.Membership);
            }
            else
            {
                // Обновление существующего
                user.Membership.Type = type;
                user.Membership.StartDate = DateTime.Now;
                user.Membership.EndDate = DateTime.Now.AddMonths(durationMonths);
                user.Membership.Price = price;
                user.Membership.IsActive = true;
                user.Membership.RemainingVisits = type == MembershipType.OneTime ? 1 : 0;
            }

            await _context.SaveChangesAsync();
            return user.Membership;
        }

        // Метод поиска клиента, для отметки посещения
        public async Task<List<User>> SearchClients(string searchTerm)
        {
            var query = _context.Users
                .Include(u => u.Membership)
                .Include(u => u.Attendances)
                .Where(u => u.Role == UserRole.Client && u.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.Phone.Contains(searchTerm));
            }

            return await query.OrderBy(u => u.LastName).ToListAsync();
        }

        // Метод отметки посещения
        public async Task<Attendance> MarkAttendance(int userId, string adminName, string notes = null)
        {
            var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }
                
            if (user.Membership == null || !user.Membership.IsValid())
            {
                throw new Exception("У пользователя нет активного абонемента");
            }

            var today = DateTime.Today;
            if (await _context.Attendances.AnyAsync(a => a.UserId == userId && a.CheckInTime.Date == today))
            {
                throw new Exception("Пользователь уже отметился сегодня");
            }


            // У разовых абонементов кол-во посещений <= 1

            if (user.Membership.Type == MembershipType.OneTime)
            {
                user.Membership.RemainingVisits--;
                if (user.Membership.RemainingVisits <= 0)
                {
                    user.Membership.IsActive = false;
                }
            }

            var attendance = new Attendance
            {
                UserId = userId,
                CheckInTime = DateTime.Now,
                CheckedByAdmin = adminName,
                Notes = "Посещение отмечено администратором"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return attendance;
        }

        // Метод получения статистики клиента
        public async Task<object> GetStatistics()
        {
            var totalClients = await _context.Users.CountAsync(u => u.Role == UserRole.Client);

            var activeMemberships = await _context.Memberships.CountAsync(m => m.IsActive && m.EndDate > DateTime.Now);

            var todayVisits = await _context.Attendances.CountAsync(a => a.CheckInTime.Date == DateTime.Today);

            var monthlyRevenue = await _context.Memberships.Where(m => m.StartDate.Month == DateTime.Now.Month).Select(m => (double?)m.Price).SumAsync() ?? 0;
            
            return new
            {
                TotalClients = totalClients,
                ActiveMemberships = activeMemberships,
                TodayVisits = todayVisits,
                MonthlyRevenue = monthlyRevenue
            };
        }

        // Метод получения статистики посещений клиента
        public async Task<List<Attendance>> GetClientAttendances(int userId)
        {
            return await _context.Attendances.Where(a => a.UserId == userId).OrderByDescending(a => a.CheckInTime).ToListAsync();
        }
    }
}
