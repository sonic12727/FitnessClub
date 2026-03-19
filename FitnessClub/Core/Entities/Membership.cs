using FitnessClub.Core.Enums;

namespace FitnessClub.Core.Entities
{
    public class Membership
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public MembershipType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int RemainingVisits { get; set; } // Для разовых абонементов

        public bool IsValid()
        {
            bool isValid = IsActive && EndDate > DateTime.Now;

            // Для разовых абонементов дополнительная проверка
            if (Type == MembershipType.OneTime)
            {
                isValid = isValid && RemainingVisits > 0;
            }
            Console.WriteLine(isValid ? "Абонемент активен!" : "Абонемент неактивен!");
            return isValid;
        }

        public User User { get; set; }
    }
}
