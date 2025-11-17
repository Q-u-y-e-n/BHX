using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("PhieuNhap_Tong")]
    public class PhieuNhap_Tong
    {
        [Key]
        public int PhieuNhapID { get; set; }

        public int NhaCungCapID { get; set; }

        public DateTime NgayNhap { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [ForeignKey(nameof(NhaCungCapID))]
        public NhaCungCap? NhaCungCap { get; set; }

        public ICollection<ChiTietNhap_Tong>? ChiTietNhapTongs { get; set; }
    }
}
