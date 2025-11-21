using System.ComponentModel.DataAnnotations;

namespace BHX_Web.ViewModels
{
    public class PhieuNhapViewModel
    {
        // Bỏ NhaCungCapID ở đây vì mỗi dòng có thể là 1 NCC khác nhau
        [Required(ErrorMessage = "Vui lòng chọn ngày nhập")]
        public DateTime NgayNhap { get; set; } = DateTime.Now;

        public List<ChiTietNhapItem> ChiTiets { get; set; } = new List<ChiTietNhapItem>();
    }

    public class ChiTietNhapItem
    {
        // 👇 Chuyển NCC vào đây
        [Required(ErrorMessage = "Chọn NCC")]
        public int NhaCungCapID { get; set; }

        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
}