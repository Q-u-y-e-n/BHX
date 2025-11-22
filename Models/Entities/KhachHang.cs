using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int KhachHangID { get; set; }

        [Required]
        [StringLength(200)]
        public string TenKhachHang { get; set; } = string.Empty;

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(300)]
        public string? DiaChi { get; set; }

        // Navigation
        public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
        public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
    }
}