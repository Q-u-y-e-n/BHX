using System.ComponentModel.DataAnnotations;

namespace BHX_Web.ViewModels
{
    public class TraHangViewModel
    {
        public List<TraHangItem> ChiTiets { get; set; } = new List<TraHangItem>();
    }

    public class TraHangItem
    {
        public int SanPhamID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do")]
        public string LyDo { get; set; } = "Hết hạn sử dụng"; // Mặc định
    }
}