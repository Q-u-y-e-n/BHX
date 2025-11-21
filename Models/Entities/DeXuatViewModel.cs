namespace BHX_Web.ViewModels
{
    public class DeXuatViewModel
    {
        public List<DeXuatItem> Items { get; set; } = new List<DeXuatItem>();
    }

    public class DeXuatItem
    {
        public int SanPhamID { get; set; }
        public string? TenSanPham { get; set; }
        public string? DonViTinh { get; set; }
        public string? HinhAnh { get; set; }

        public int TonKhoHienTai { get; set; }
        public int SoLuongDeXuat { get; set; }
    }
}