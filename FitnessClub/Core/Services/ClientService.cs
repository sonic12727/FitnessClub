using FitnessClub.Core.Requests;
using FitnessClub.Core.Entities;
using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessClub.Core.Services
{
    public class ClientService
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<ClientService> _logger;
        private const int DefaultWorkFactor = 12;

        public ClientService(FitnessClubDbContext context, ILogger<ClientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> CreateClientAsync(CreateClientRequest request)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                throw new Exception("Пользователь с таким email уже существует");
            }

            var client = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, DefaultWorkFactor),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Role = UserRole.Client,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _context.Users.AddAsync(client);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создан клиент {ClientId} {Email}", client.Id, client.Email);

            return client;
        }

        public async Task<List<User>> SearchClientsAsync(string search)
        {
            search = search?.Trim().ToLower() ?? string.Empty;

            return await _context.Users
                .Include(u => u.Membership)
                .Where(u => u.Role == UserRole.Client &&
                           (string.IsNullOrEmpty(search) ||
                            u.FirstName.ToLower().Contains(search) ||
                            u.LastName.ToLower().Contains(search) ||
                            u.Email.ToLower().Contains(search) ||
                            (u.Phone != null && u.Phone.Contains(search))))
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<object?> GetClientDetailsAsync(int id)
        {
            var client = await _context.Users
                .Include(u => u.Membership)
                .Include(u => u.Attendances)
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Client);

            if (client == null)
            {
                _logger.LogWarning("Клиент с ID {ClientId} не найден", id);
                return null;
            }

            return new
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
                LastVisit = client.Attendances
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefault()?.CheckInTime
            };
        }
    }
}
