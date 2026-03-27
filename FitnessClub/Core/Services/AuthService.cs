using FitnessClub.Core.Entities;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;
using FitnessClub.Core.Enums;

namespace FitnessClub.Core.Services
{
    public class AuthService
    {
        private readonly FitnessClubDbContext _context;
        private const int DefaultWorkFactor = 12;

        public AuthService(FitnessClubDbContext context)
        {
            _context = context;
        }

        public async Task<User?> Login(string email, string password)
        {
            try
            {
                var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                if (user == null)
                {
                    return null;
                }
                if (!user.IsActive)
                {
                    return null;
                }
                bool isPaswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                if (!isPaswordValid)
                {
                    return null;
                }
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                return null;
            }
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, DefaultWorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<User> Register(string email,string password,string firstName,string lastName,string phone)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (existingUser != null)
            {
                throw new Exception("Пользователь с таким email уже существует");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                Role = UserRole.Client,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}