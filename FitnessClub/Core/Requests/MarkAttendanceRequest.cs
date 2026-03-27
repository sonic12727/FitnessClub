using System.ComponentModel.DataAnnotations;

namespace FitnessClub.Core.Requests
{
    public class MarkAttendanceRequest
    {
        [Required]
        public int UserId { get; set; }
    }
}
