using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("PhieuPhanPhoi")]
    public class PhieuPhanPhoi
    {
        [Key]
        public int PhieuPhanPhoiID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? TrangThai { get; set; } // "Đang giao", "Đã nhận"

        // Navigation Properties
        [ForeignKey("CuaHangID")]
        public virtual CuaHang? CuaHang { get; set; }

        // 👇 QUAN TRỌNG: Bỏ dấu '?' và khởi tạo new List()
        public virtual ICollection<ChiTietPhanPhoi> ChiTietPhanPhois { get; set; } = new List<ChiTietPhanPhoi>();
    }
}