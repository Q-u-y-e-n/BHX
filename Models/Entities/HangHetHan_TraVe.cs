using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("HangHetHan_TraVe")]
    public class HangHetHan_TraVe
    {
        [Key]
        public int TraVeID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayTra { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        public ICollection<ChiTietHangHetHan>? ChiTietHangHetHans { get; set; }
    }
}
