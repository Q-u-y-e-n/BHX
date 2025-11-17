using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("PhieuNhap_CuaHang")]
    public class PhieuNhap_CuaHang
    {
        [Key]
        public int PhieuNhapCHID { get; set; }

        public int CuaHangID { get; set; }

        public DateTime NgayNhap { get; set; } = DateTime.Now;

        [ForeignKey(nameof(CuaHangID))]
        public CuaHang? CuaHang { get; set; }

        public ICollection<ChiTietNhap_CuaHang>? ChiTietNhap_CuaHangs { get; set; }
    }
}
