using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("UserRoles")]
    public class UserRoles
    {
        [Key]
        public int UserRoleID { get; set; }

        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual Users User { get; set; } = null!; // Bỏ qua cảnh báo null

        public int RoleID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Roles Role { get; set; } = null!; // Bỏ qua cảnh báo null
    }
}