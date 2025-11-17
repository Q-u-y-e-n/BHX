using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietDeXuatNhap")]
    public class ChiTietDeXuatNhap
    {
        [Key]
        public int ChiTietDXID { get; set; }

        public int DeXuatID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [ForeignKey(nameof(DeXuatID))]
        public DeXuatNhapHang? DeXuatNhapHang { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
