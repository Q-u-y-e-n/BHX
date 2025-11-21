using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("SanPham")]
    public class SanPham
    {
        // ==========================
        // 1. THUỘC TÍNH CƠ BẢN (Khớp với SQL)
        // ==========================

        [Key]
        public int SanPhamID { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string TenSanPham { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Đơn vị tính")]
        public string? DonViTinh { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá nhập")]
        public decimal GiaNhap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá bán")]
        public decimal GiaBan { get; set; }

        // 👇 Cột lưu đường dẫn ảnh (VD: /images/products/abc.jpg)
        [StringLength(500)]
        [Display(Name = "Hình ảnh")]
        public string? HinhAnh { get; set; }

        // 👇 Cột phân loại sản phẩm
        [StringLength(100)]
        [Display(Name = "Loại sản phẩm")]
        public string? LoaiSanPham { get; set; }

        // ==========================================================
        // 2. NAVIGATION PROPERTIES (Quan hệ với bảng khác)
        // ==========================================================

        // Dùng virtual để hỗ trợ Lazy Loading của Entity Framework
        public virtual ICollection<KhoTong>? KhoTongs { get; set; }
        public virtual ICollection<ChiTietNhap_Tong>? ChiTietNhapTongs { get; set; }
        public virtual ICollection<ChiTietPhanPhoi>? ChiTietPhanPhois { get; set; }
        public virtual ICollection<BanHang_TongHop>? BanHang_TongHops { get; set; }
        public virtual ICollection<ChiTietHangHetHan>? ChiTietHangHetHans { get; set; }
        public virtual ICollection<TonKho_CuaHang>? TonKho_CuaHangs { get; set; }
        public virtual ICollection<ChiTietDeXuatNhap>? ChiTietDeXuatNhaps { get; set; }
        public virtual ICollection<ChiTietNhap_CuaHang>? ChiTietNhap_CuaHangs { get; set; }
        public virtual ICollection<ChiTietTraHang>? ChiTietTraHangs { get; set; }
        public virtual ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; }
        public virtual ICollection<ChiTietDonHang>? ChiTietDonHangs { get; set; }
    }
}