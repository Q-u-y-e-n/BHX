using System.ComponentModel.DataAnnotations;

namespace BHX_Web.ViewModels
{
    public class SanPhamViewModel
    {
        public int SanPhamID { get; set; }

        [Display(Name = "Tên sản phẩm")]
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string TenSanPham { get; set; } = string.Empty;

        [Display(Name = "Đơn vị tính")]
        [Required(ErrorMessage = "Vui lòng nhập ĐVT")]
        public string DonViTinh { get; set; } = string.Empty;

        [Display(Name = "Giá nhập")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn 0")]
        public decimal GiaNhap { get; set; }

        [Display(Name = "Giá bán")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
        public decimal GiaBan { get; set; }

        [Display(Name = "Loại sản phẩm")]
        public string? LoaiSanPham { get; set; }

        // --- Phần Hình Ảnh (Cái này SanPham.cs không có) ---
        [Display(Name = "Hình ảnh")]
        public IFormFile? HinhAnhFile { get; set; } // File upload từ máy tính

        public string? HinhAnhHienTai { get; set; } // Đường dẫn ảnh cũ (khi sửa)

        // --- Phần Kho (Cái này SanPham.cs cũng không có) ---
        [Display(Name = "Số lượng tồn")]
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm")]
        public int SoLuong { get; set; }
    }
}