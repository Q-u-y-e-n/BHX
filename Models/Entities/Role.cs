using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("Roles")]
    public class Roles
    {
        [Key]
        public int RoleID { get; set; }

        [Required(ErrorMessage = "Tên vai trò là bắt buộc")]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; } // Cho phép NULL

        // Quan hệ: Một Role có nhiều UserRoles
        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}