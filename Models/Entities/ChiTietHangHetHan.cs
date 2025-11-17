using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietHangHetHan")]
    public class ChiTietHangHetHan
    {
        [Key]
        public int ChiTietTraID { get; set; }

        public int TraVeID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaTriHuHong { get; set; }

        [ForeignKey(nameof(TraVeID))]
        public HangHetHan_TraVe? HangHetHan_TraVe { get; set; }

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
