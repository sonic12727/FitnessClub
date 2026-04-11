using FitnessClub.Core.Enums;

namespace FitnessClub.Core.Entities
{
    public class Membership
    {
        public User User { get; set; }
        public int Id { get; set; }
        public int UserId { get; set; }
        public MembershipType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int RemainingVisits { get; set; } // Для количественных абонементов

        public bool IsTimeBased()
        {
            return Type == MembershipType.Monthly || Type == MembershipType.Quarterly || Type == MembershipType.Yearly;
        }

        public bool IsVisitBased()
        {
            return Type == MembershipType.OneTime || Type == MembershipType.Visits8 || Type == MembershipType.Visits12;
        }

        public int GetInitialVisitsCount()
        {
            return Type switch
            {
                MembershipType.OneTime => 1,
                MembershipType.Visits8 => 8,
                MembershipType.Visits12 => 12,
                _ => 0
            };
        }

        public bool IsValid()
        {
            if (!IsActive)
                return false;

            if (IsTimeBased())
                return EndDate > DateTime.UtcNow;

            if (IsVisitBased())
                return RemainingVisits > 0;

            return false;
        }
    }
}
