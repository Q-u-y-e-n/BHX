using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int KhachHangID { get; set; }

        [MaxLength(200)]
        public string? TenKhachHang { get; set; }

        [MaxLength(20)]
        public string? SoDienThoai { get; set; }

        [MaxLength(300)]
        public string? DiaChi { get; set; }

        public ICollection<HoaDon>? HoaDons { get; set; }
        public ICollection<DonHang>? DonHangs { get; set; }
    }
}
