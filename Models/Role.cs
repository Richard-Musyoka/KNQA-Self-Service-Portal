using System.ComponentModel.DataAnnotations;

namespace KNQASelfService.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public virtual ICollection<User>? Users { get; set; }
    }
}