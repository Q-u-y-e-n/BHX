using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietTraHang")]
    public class ChiTietTraHang
    {
        [Key]
        public int ChiTietTraCHID { get; set; }

        public int TraHangID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [MaxLength(300)]
        public string? LyDo { get; set; }

        [ForeignKey(nameof(TraHangID))]
        public DanhSachTraHang? DanhSachTraHang { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
