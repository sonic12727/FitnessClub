using FitnessClub.Core.Enums;

namespace FitnessClub.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }

        public Membership Membership { get; set; }
        public List<Attendance> Attendances { get; set; } = new();
    }
}
