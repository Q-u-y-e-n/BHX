using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietNhap_CuaHang")]
    public class ChiTietNhap_CuaHang
    {
        [Key]
        public int ChiTietNhapCHID { get; set; }

        public int PhieuNhapCHID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [ForeignKey(nameof(PhieuNhapCHID))]
        public PhieuNhap_CuaHang? PhieuNhap_CuaHang { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
