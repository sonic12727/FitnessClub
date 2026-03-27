namespace FitnessClub.Core.Entities
{
    public class Attendance
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CheckInTime { get; set; }
        public string CheckedByAdmin { get; set; } = string.Empty; // Кто отметил
        public string? Notes { get; set; }
        public User User { get; set; }
    }
}
