using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KNQASelfService.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string? UserNameId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(256)]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        [NotMapped]
        [Required]
        [DataType(DataType.Password)]
        [Compare("PasswordHash", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public int RoleId { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsVerified { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public bool ChangePassword { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public string EmployeeNo { get; set; }


        // Navigation property (if you have a Role model)
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }
    }
}