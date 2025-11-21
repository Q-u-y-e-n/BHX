using System.ComponentModel.DataAnnotations;

namespace BHX_Web.ViewModels
{
    public class CreateStoreViewModel
    {
        // --- Thông tin Cửa hàng ---
        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng")]
        public string TenCuaHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string DiaChi { get; set; } = string.Empty;

        public string? SoDienThoai { get; set; } = string.Empty;

        // --- Thông tin Tài khoản Quản lý ---
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập quản lý")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập không được chứa ký tự đặc biệt")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;
    }
}