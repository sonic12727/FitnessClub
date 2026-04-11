using System.ComponentModel.DataAnnotations;

namespace FitnessClub.Core.Requests
{
    public class UpdateClientRequest
    {
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string? Password { get; set; }
    }
}
