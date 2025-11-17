using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("DanhSachTraHang")]
    public class DanhSachTraHang
    {
        [Key]
        public int TraHangID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayTra { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        public ICollection<ChiTietTraHang>? ChiTietTraHangs { get; set; }
    }
}
