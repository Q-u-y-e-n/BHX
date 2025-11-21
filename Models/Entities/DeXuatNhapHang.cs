using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("DeXuatNhapHang")]
    public class DeXuatNhapHang
    {
        [Key]
        public int DeXuatID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayDeXuat { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? TrangThai { get; set; } // Chờ duyệt, Đã duyệt, Từ chối

        // Navigation
        [ForeignKey("CuaHangID")]
        public virtual CuaHang? CuaHang { get; set; }

        // 👇👇👇 SỬA DÒNG NÀY 👇👇👇
        // 1. Bỏ dấu '?'
        // 2. Thêm '= new List<ChiTietDeXuatNhap>();'
        public virtual ICollection<ChiTietDeXuatNhap> ChiTietDeXuatNhaps { get; set; } = new List<ChiTietDeXuatNhap>();
    }
}