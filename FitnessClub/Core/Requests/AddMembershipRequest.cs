using System.ComponentModel.DataAnnotations;

namespace FitnessClub.Core.Requests
{
    public class AddMembershipRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        [Range(1, 36)]
        public int DurationMonths { get; set; }

        [Range(0, 1000000)]
        public decimal Price { get; set; }
    }
}
