using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("PhieuNhap_Tong")]
    public class PhieuNhap_Tong
    {
        [Key]
        public int PhieuNhapID { get; set; }

        public int NhaCungCapID { get; set; }

        public DateTime NgayNhap { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        // Navigation Properties
        [ForeignKey("NhaCungCapID")]
        public virtual NhaCungCap? NhaCungCap { get; set; }

        // 👇 SỬA Ở ĐÂY: Bỏ dấu '?' và thêm '= new List...();'
        // Điều này giúp sửa lỗi CS8620 trong Controller
        public virtual ICollection<ChiTietNhap_Tong> ChiTietNhapTongs { get; set; } = new List<ChiTietNhap_Tong>();
    }
}