using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("UserRoles")]
    public class UserRoles
    {
        [Key]
        public int UserRoleID { get; set; }

        // Khóa ngoại tới Bảng Users
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual Users User { get; set; } = null!; // <--- SỬA Ở ĐÂY

        // Khóa ngoại tới Bảng Roles
        public int RoleID { get; set; }
        [ForeignKey("RoleID")]
        public virtual Roles Role { get; set; } = null!; // <--- SỬA Ở ĐÂY
    }
}