namespace FitnessClub.Core.Entities
{
    public class Callback
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsProcessed { get; set; }
    }
}
