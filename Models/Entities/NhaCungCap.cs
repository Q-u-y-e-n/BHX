using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("NhaCungCap")]
    public class NhaCungCap
    {
        [Key]
        public int NhaCungCapID { get; set; }

        [MaxLength(200)]
        public string? TenNCC { get; set; }

        [MaxLength(300)]
        public string? DiaChi { get; set; }

        [MaxLength(20)]
        public string? SoDienThoai { get; set; }

        // Navigation
        public ICollection<PhieuNhap_Tong>? PhieuNhaps { get; set; }
    }
}
