using Microsoft.EntityFrameworkCore;
using BHX_Web.Models.Entities;

namespace BHX_Web.Data
{
    public class BHXContext : DbContext
    {
        public BHXContext(DbContextOptions<BHXContext> options) : base(options) { }

        // ===========================================================
        // 1. DANH SÁCH BẢNG NGHIỆP VỤ (Dùng để truy vấn LINQ)
        // ===========================================================

        // --- Quản lý chung ---
        public DbSet<CuaHang> CuaHangs { get; set; }
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }

        // --- Kho Tổng Công Ty ---
        public DbSet<KhoTong> KhoTongs { get; set; }
        public DbSet<PhieuNhap_Tong> PhieuNhap_Tongs { get; set; }
        public DbSet<ChiTietNhap_Tong> ChiTietNhap_Tongs { get; set; }

        // --- Phân Phối ---
        public DbSet<PhieuPhanPhoi> PhieuPhanPhois { get; set; }
        public DbSet<ChiTietPhanPhoi> ChiTietPhanPhois { get; set; }

        // --- Kho Cửa Hàng ---
        public DbSet<TonKho_CuaHang> TonKho_CuaHangs { get; set; }
        public DbSet<DeXuatNhapHang> DeXuatNhapHangs { get; set; }
        public DbSet<ChiTietDeXuatNhap> ChiTietDeXuatNhaps { get; set; }

        // --- Nghiệp vụ Cửa Hàng ---
        public DbSet<PhieuNhap_CuaHang> PhieuNhap_CuaHangs { get; set; }
        public DbSet<ChiTietNhap_CuaHang> ChiTietNhap_CuaHangs { get; set; }
        public DbSet<DanhSachTraHang> DanhSachTraHangs { get; set; }
        public DbSet<ChiTietTraHang> ChiTietTraHangs { get; set; }

        // --- Bán Hàng ---
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<BanHang_TongHop> BanHang_TongHops { get; set; }

        // --- Khách Hàng Online (Mới bổ sung) ---
        public DbSet<GioHang> GioHangs { get; set; } // <--- QUAN TRỌNG: Bảng Giỏ Hàng
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

        // --- Xử lý hàng lỗi ---
        public DbSet<HangHetHan_TraVe> HangHetHan_TraVes { get; set; }
        public DbSet<ChiTietHangHetHan> ChiTietHangHetHans { get; set; }

        // ===========================================================
        // 2. BẢNG PHÂN QUYỀN NGƯỜI DÙNG
        // ===========================================================
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<UserRoles> UserRoles { get; set; }

        // ===========================================================
        // 3. CẤU HÌNH FLUENT API (Thiết lập ràng buộc)
        // ===========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- Mapping Tên Bảng (Để khớp với SQL Server) -----
            modelBuilder.Entity<PhieuNhap_Tong>().ToTable("PhieuNhap_Tong");
            modelBuilder.Entity<ChiTietNhap_Tong>().ToTable("ChiTietNhap_Tong");
            modelBuilder.Entity<BanHang_TongHop>().ToTable("BanHang_TongHop");
            modelBuilder.Entity<HangHetHan_TraVe>().ToTable("HangHetHan_TraVe");
            modelBuilder.Entity<ChiTietHangHetHan>().ToTable("ChiTietHangHetHan");
            modelBuilder.Entity<TonKho_CuaHang>().ToTable("TonKho_CuaHang");
            modelBuilder.Entity<DeXuatNhapHang>().ToTable("DeXuatNhapHang");
            modelBuilder.Entity<ChiTietDeXuatNhap>().ToTable("ChiTietDeXuatNhap");
            modelBuilder.Entity<PhieuNhap_CuaHang>().ToTable("PhieuNhap_CuaHang");
            modelBuilder.Entity<ChiTietNhap_CuaHang>().ToTable("ChiTietNhap_CuaHang");
            modelBuilder.Entity<DanhSachTraHang>().ToTable("DanhSachTraHang");
            modelBuilder.Entity<ChiTietTraHang>().ToTable("ChiTietTraHang");
            modelBuilder.Entity<ChiTietPhanPhoi>().ToTable("ChiTietPhanPhoi");
            modelBuilder.Entity<ChiTietHoaDon>().ToTable("ChiTietHoaDon");
            modelBuilder.Entity<ChiTietDonHang>().ToTable("ChiTietDonHang");

            modelBuilder.Entity<GioHang>().ToTable("GioHang"); // <--- Mapping bảng Giỏ Hàng

            modelBuilder.Entity<Users>().ToTable("Users");
            modelBuilder.Entity<Roles>().ToTable("Roles");
            modelBuilder.Entity<UserRoles>().ToTable("UserRoles");

            // ----- Ràng buộc Unique (Duy nhất) -----
            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Roles>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // ----- Quan hệ nhiều - nhiều (UserRoles) -----
            modelBuilder.Entity<UserRoles>()
                .HasKey(ur => ur.UserRoleID);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}