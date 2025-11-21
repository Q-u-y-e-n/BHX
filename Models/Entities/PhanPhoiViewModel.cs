using System.ComponentModel.DataAnnotations;

namespace BHX_Web.ViewModels
{
    public class PhanPhoiViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn Cửa hàng nhận")]
        public int CuaHangID { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public string? GhiChu { get; set; }

        public List<ChiTietPhanPhoiItem> ChiTiets { get; set; } = new List<ChiTietPhanPhoiItem>();
    }

    public class ChiTietPhanPhoiItem
    {
        public int SanPhamID { get; set; }
        public int SoLuong { get; set; }
    }
}