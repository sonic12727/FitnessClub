using System.ComponentModel.DataAnnotations;

namespace FitnessClub.Core.Requests
{
    public class AddMembershipRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;
    }
}
