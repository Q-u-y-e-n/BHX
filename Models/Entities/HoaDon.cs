using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        public int HoaDonID { get; set; }

        public int CuaHangID { get; set; }
        public int KhachHangID { get; set; }

        public DateTime NgayLap { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        [ForeignKey(nameof(KhachHangID))]
        public KhachHang? KhachHang { get; set; }

        public ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; }
    }
}
