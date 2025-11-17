using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("PhieuPhanPhoi")]
    public class PhieuPhanPhoi
    {
        [Key]
        public int PhieuPhanPhoiID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        public ICollection<ChiTietPhanPhoi>? ChiTietPhanPhois { get; set; }
    }
}
