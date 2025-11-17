using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("DonHang")]
    public class DonHang
    {
        [Key]
        public int DonHangID { get; set; }

        public int KhachHangID { get; set; }

        public DateTime NgayDat { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [ForeignKey(nameof(KhachHangID))]
        public KhachHang? KhachHang { get; set; }

        public ICollection<ChiTietDonHang>? ChiTietDonHangs { get; set; }
    }
}
