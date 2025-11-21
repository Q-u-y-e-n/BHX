using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("Roles")]
    public class Roles
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        // Khởi tạo List rỗng để tránh lỗi Null Reference khi gọi Roles.UserRoles
        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}