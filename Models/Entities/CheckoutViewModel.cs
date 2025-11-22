using System.ComponentModel.DataAnnotations;

namespace BHX_Web.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string TenNguoiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChi { get; set; } = string.Empty;

        public string? GhiChu { get; set; }

        // Để hiển thị lại bên phải màn hình
        public List<GioHangItem>? CartItems { get; set; }
        public decimal TongTien { get; set; }
    }
}