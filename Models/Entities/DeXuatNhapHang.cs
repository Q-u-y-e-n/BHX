using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("DeXuatNhapHang")]
    public class DeXuatNhapHang
    {
        [Key]
        public int DeXuatID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayDeXuat { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        public ICollection<ChiTietDeXuatNhap>? ChiTietDeXuatNhaps { get; set; }
    }
}
