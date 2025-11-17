using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("BanHang_TongHop")]
    public class BanHang_TongHop
    {
        [Key]
        public int BanHangID { get; set; }

        public int CuaHangID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuongBan { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DoanhThu { get; set; }

        public int Thang { get; set; }
        public int Nam { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
