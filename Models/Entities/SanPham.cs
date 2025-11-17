using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        public int SanPhamID { get; set; }

        [MaxLength(200)]
        public string? TenSanPham { get; set; }

        [MaxLength(50)]
        public string? DonViTinh { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaNhap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaBan { get; set; }

        // Navigation
        public ICollection<KhoTong>? KhoTongs { get; set; }
        public ICollection<ChiTietNhap_Tong>? ChiTietNhapTongs { get; set; }
        public ICollection<ChiTietPhanPhoi>? ChiTietPhanPhois { get; set; }
        public ICollection<BanHang_TongHop>? BanHang_TongHops { get; set; }
        public ICollection<ChiTietHangHetHan>? ChiTietHangHetHans { get; set; }
        public ICollection<TonKho_CuaHang>? TonKho_CuaHangs { get; set; }
        public ICollection<ChiTietDeXuatNhap>? ChiTietDeXuatNhaps { get; set; }
        public ICollection<ChiTietNhap_CuaHang>? ChiTietNhap_CuaHangs { get; set; }
        public ICollection<ChiTietTraHang>? ChiTietTraHangs { get; set; }
        public ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; }
        public ICollection<ChiTietDonHang>? ChiTietDonHangs { get; set; }
    }
}
