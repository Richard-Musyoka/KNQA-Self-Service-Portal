using System.ComponentModel.DataAnnotations;

namespace KNQASelfService.Models
{
    public class OtpVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string OtpCode { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }
    }
}