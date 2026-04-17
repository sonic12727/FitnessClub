using System.ComponentModel.DataAnnotations;

namespace FitnessClub.Core.Requests
{
    public class CreateCallbackRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;
    }
}
