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

        // 👇 MỚI THÊM: Đơn hàng này thuộc về cửa hàng nào?
        public int? CuaHangID { get; set; }

        public DateTime NgayDat { get; set; } = DateTime.Now;
        [StringLength(50)]
        public string TrangThai { get; set; } = "Chờ xác nhận";

        [ForeignKey("KhachHangID")]
        public virtual KhachHang? KhachHang { get; set; }

        // 👇 MỚI THÊM: Navigation
        [ForeignKey("CuaHangID")]
        public virtual CuaHang? CuaHang { get; set; }

        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
    }
}