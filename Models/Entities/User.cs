using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("Users")]
    public class Users
    {
        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; } // Cho phép NULL

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; } // Cho phép NULL

        public bool IsActive { get; set; } = true;

        // Quan hệ: Một User có nhiều UserRoles
        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}