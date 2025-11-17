using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("TonKho_CuaHang")]
    public class TonKho_CuaHang
    {
        [Key]
        public int TonKhoID { get; set; }

        public int CuaHangID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
