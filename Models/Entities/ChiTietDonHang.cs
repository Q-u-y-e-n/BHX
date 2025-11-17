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

        [ForeignKey(nameof(DonHangID))]
        public DonHang? DonHang { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
