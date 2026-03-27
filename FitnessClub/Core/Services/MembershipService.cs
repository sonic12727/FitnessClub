using FitnessClub.Core.Entities;
using FitnessClub.Core.Enums;
using FitnessClub.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessClub.Core.Services
{
    public class MembershipService
    {
        private readonly FitnessClubDbContext _context;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(FitnessClubDbContext context, ILogger<MembershipService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Membership> AddMembershipAsync(int userId,MembershipType membershipType,int durationMonths,decimal price)
        {
            var user = await _context.Users
                .Include(u => u.Membership)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            if (user.Role != UserRole.Client)
            {
                throw new Exception("Абонемент можно выдавать только клиенту");
            }

            var now = DateTime.UtcNow;

            var endDate = membershipType switch
            {
                MembershipType.OneTime => now.AddMonths(1),
                MembershipType.Monthly => now.AddMonths(durationMonths > 0 ? durationMonths : 1),
                MembershipType.Quarterly => now.AddMonths(durationMonths > 0 ? durationMonths : 3),
                MembershipType.Yearly => now.AddMonths(durationMonths > 0 ? durationMonths : 12),
                _ => now.AddMonths(1)
            };

            var remainingVisits = membershipType == MembershipType.OneTime ? 1 : 0;

            if (user.Membership == null)
            {
                user.Membership = new Membership
                {
                    UserId = user.Id,
                    Type = membershipType,
                    StartDate = now,
                    EndDate = endDate,
                    Price = price,
                    IsActive = true,
                    RemainingVisits = remainingVisits
                };

                await _context.Memberships.AddAsync(user.Membership);
            }
            else
            {
                user.Membership.Type = membershipType;
                user.Membership.StartDate = now;
                user.Membership.EndDate = endDate;
                user.Membership.Price = price;
                user.Membership.IsActive = true;
                user.Membership.RemainingVisits = remainingVisits;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Пользователю {UserId} выдан/обновлен абонемент {MembershipType}",
                userId,
                membershipType);

            return user.Membership;
        }
    }
}
