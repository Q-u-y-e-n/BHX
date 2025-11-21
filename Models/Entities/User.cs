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
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(64)] // Khớp với VARBINARY(64) trong SQL
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [StringLength(200)]
        public string? HoTen { get; set; } // Cho phép null (nullable)

        [StringLength(20)]
        public string? SoDienThoai { get; set; } // Cho phép null

        [StringLength(50)]
        public string LoaiTaiKhoan { get; set; } = "Customer"; // Mặc định là Khách hàng

        [StringLength(50)]
        public string TrangThai { get; set; } = "Hoạt động";

        // =========================================================
        // 👇 BỔ SUNG QUAN TRỌNG: LIÊN KẾT VỚI CỬA HÀNG 👇
        // =========================================================

        // Lưu ID cửa hàng mà user này quản lý (Admin/Khách thì để null)
        public int? CuaHangID { get; set; }

        // Navigation Property: Để từ User có thể chấm sang lấy tên cửa hàng (.CuaHang.TenCuaHang)
        [ForeignKey("CuaHangID")]
        public virtual CuaHang? CuaHang { get; set; }

        // =========================================================
        // 👇 QUAN HỆ PHÂN QUYỀN 👇
        // =========================================================

        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}