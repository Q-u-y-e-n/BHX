using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("GioHang")]
    public class GioHang
    {
        [Key]
        public int GioHangID { get; set; }
        public int UserID { get; set; }
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        [ForeignKey("SanPhamID")]
        public virtual SanPham? SanPham { get; set; }
    }
}