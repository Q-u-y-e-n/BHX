using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietNhap_Tong")]
    public class ChiTietNhap_Tong
    {
        [Key]
        public int ChiTietNhapID { get; set; }

        public int PhieuNhapID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [ForeignKey(nameof(PhieuNhapID))]
        public PhieuNhap_Tong? PhieuNhap_Tong { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
