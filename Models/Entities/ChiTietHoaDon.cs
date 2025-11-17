using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietHoaDon")]
    public class ChiTietHoaDon
    {
        [Key]
        public int ChiTietHDID { get; set; }

        public int HoaDonID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [ForeignKey(nameof(HoaDonID))]
        public HoaDon? HoaDon { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
