using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("KhoTong")]
    public class KhoTong
    {
        [Key]
        public int KhoTongID { get; set; }

        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }

        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        [ForeignKey(nameof(SanPhamID))]
        public SanPham? SanPham { get; set; }
    }
}
