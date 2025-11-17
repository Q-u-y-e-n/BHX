using Microsoft.EntityFrameworkCore;
using BHX_Web.Models.Entities;

namespace BHX_Web.Data
{
    public class BHXContext : DbContext
    {
        public BHXContext(DbContextOptions<BHXContext> options) : base(options) { }

        // ----- DANH SÁCH BẢNG NGHIỆP VỤ (CŨ CỦA BẠN) -----
        public DbSet<CuaHang> CuaHangs { get; set; }
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<KhoTong> KhoTongs { get; set; }
        public DbSet<PhieuNhap_Tong> PhieuNhap_Tongs { get; set; }
        public DbSet<ChiTietNhap_Tong> ChiTietNhap_Tongs { get; set; }
        public DbSet<PhieuPhanPhoi> PhieuPhanPhois { get; set; }
        public DbSet<ChiTietPhanPhoi> ChiTietPhanPhois { get; set; }
        public DbSet<BanHang_TongHop> BanHang_TongHops { get; set; }
        public DbSet<HangHetHan_TraVe> HangHetHan_TraVes { get; set; }
        public DbSet<ChiTietHangHetHan> ChiTietHangHetHans { get; set; }
        public DbSet<TonKho_CuaHang> TonKho_CuaHangs { get; set; }
        public DbSet<DeXuatNhapHang> DeXuatNhapHangs { get; set; }
        public DbSet<ChiTietDeXuatNhap> ChiTietDeXuatNhaps { get; set; }
        public DbSet<PhieuNhap_CuaHang> PhieuNhap_CuaHangs { get; set; }
        public DbSet<ChiTietNhap_CuaHang> ChiTietNhap_CuaHangs { get; set; }
        public DbSet<DanhSachTraHang> DanhSachTraHangs { get; set; }
        public DbSet<ChiTietTraHang> ChiTietTraHangs { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

        // ===== THÊM 3 BẢNG PHÂN QUYỀN MỚI =====
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<UserRoles> UserRoles { get; set; }
        // ======================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- CẤU HÌNH .ToTable() CŨ CỦA BẠN -----
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

            // ===== THÊM CẤU HÌNH CHO CÁC BẢNG MỚI =====
            modelBuilder.Entity<Users>().ToTable("Users");
            modelBuilder.Entity<Roles>().ToTable("Roles");
            modelBuilder.Entity<UserRoles>().ToTable("UserRoles");

            // Cấu hình ràng buộc UNIQUE (vì bạn đã định nghĩa trong SQL)
            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Roles>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // Cấu hình quan hệ nhiều-nhiều giữa Users và Roles
            // thông qua bảng UserRoles
            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserID);

            modelBuilder.Entity<UserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleID);
            // ==========================================
        }
    }
}