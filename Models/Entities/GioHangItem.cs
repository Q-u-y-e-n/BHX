namespace BHX_Web.ViewModels
{
    public class GioHangItem
    {
        public int SanPhamID { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}