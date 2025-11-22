using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietDonHang")]
    public class ChiTietDonHang
    {
        [Key]
        public int ChiTietDonHangID { get; set; }

        public int DonHangID { get; set; }

        public int SanPhamID { get; set; }

        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        // Navigation
        [ForeignKey("DonHangID")]
        public virtual DonHang? DonHang { get; set; }

        [ForeignKey("SanPhamID")]
        public virtual SanPham? SanPham { get; set; }
    }
}