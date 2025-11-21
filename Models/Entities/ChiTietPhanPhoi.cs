using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("ChiTietPhanPhoi")]
    public class ChiTietPhanPhoi
    {
        [Key]
        public int ChiTietPhanPhoiID { get; set; }

        public int PhieuPhanPhoiID { get; set; }

        public int SanPhamID { get; set; }

        public int SoLuong { get; set; }

        // Navigation
        [ForeignKey("PhieuPhanPhoiID")]
        public virtual PhieuPhanPhoi? PhieuPhanPhoi { get; set; }

        [ForeignKey("SanPhamID")]
        public virtual SanPham? SanPham { get; set; }
    }
}