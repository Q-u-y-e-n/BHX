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

        // Navigation
        [ForeignKey("DeXuatID")]
        public virtual DeXuatNhapHang? DeXuatNhapHang { get; set; }

        [ForeignKey("SanPhamID")]
        public virtual SanPham? SanPham { get; set; }
    }
}