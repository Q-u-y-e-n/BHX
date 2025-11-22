namespace BHX_Web.ViewModels
{
    public class ThongKeViewModel
    {
        public int Thang { get; set; }
        public int Nam { get; set; }

        // Các con số tổng quan
        public decimal TongDoanhThu { get; set; }
        public int TongDonHang { get; set; }
        public decimal TongGiaTriTraHang { get; set; } // Tiền hàng bị hỏng/trả về

        // Chi tiết từng sản phẩm bán ra
        public List<ChiTietBanHang> SanPhamBanChay { get; set; } = new List<ChiTietBanHang>();
    }

    public class ChiTietBanHang
    {
        public int SanPhamID { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
    }
}