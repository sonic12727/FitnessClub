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

        public async Task<Membership> AddMembershipAsync(int userId, MembershipType membershipType)
        {
            var user = await _context.Users.Include(u => u.Membership).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new Exception("Пользователь не найден");

            if (user.Role != UserRole.Client)
                throw new Exception("Абонемент можно выдавать только клиенту");

            var now = DateTime.UtcNow;

            decimal price;
            DateTime endDate;
            int remainingVisits;
            bool isTimeBased;
            bool isVisitBased;

            switch (membershipType)
            {
                case MembershipType.OneTime:
                    price = 3000;
                    endDate = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
                    remainingVisits = 1;
                    isTimeBased = false;
                    isVisitBased = true;
                    break;

                case MembershipType.Visits8:
                    price = 12000;
                    endDate = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
                    remainingVisits = 8;
                    isTimeBased = false;
                    isVisitBased = true;
                    break;

                case MembershipType.Visits12:
                    price = 16000;
                    endDate = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
                    remainingVisits = 12;
                    isTimeBased = false;
                    isVisitBased = true;
                    break;

                case MembershipType.Monthly:
                    price = 20000;
                    endDate = now.AddMonths(1);
                    remainingVisits = 0;
                    isTimeBased = true;
                    isVisitBased = false;
                    break;

                case MembershipType.Quarterly:
                    price = 40000;
                    endDate = now.AddMonths(3);
                    remainingVisits = 0;
                    isTimeBased = true;
                    isVisitBased = false;
                    break;

                case MembershipType.Yearly:
                    price = 100000;
                    endDate = now.AddMonths(12);
                    remainingVisits = 0;
                    isTimeBased = true;
                    isVisitBased = false;
                    break;

                default:
                    throw new Exception("Неподдерживаемый тип абонемента");
            }

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
