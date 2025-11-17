using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHX_Web.Models.Entities
{
    [Table("CuaHang")]
    public class CuaHang
    {
        [Key]
        public int CuaHangID { get; set; }

        [MaxLength(200)]
        public string? TenCuaHang { get; set; }

        [MaxLength(300)]
        public string? DiaChi { get; set; }

        [MaxLength(20)]
        public string? SoDienThoai { get; set; }

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        // Navigation
        public ICollection<PhieuPhanPhoi>? PhieuPhanPhois { get; set; }
        public ICollection<TonKho_CuaHang>? TonKhoCuaHangs { get; set; }
        public ICollection<DeXuatNhapHang>? DeXuatNhaps { get; set; }
        public ICollection<PhieuNhap_CuaHang>? PhieuNhapCuaHangs { get; set; }
        public ICollection<DanhSachTraHang>? DanhSachTraHangs { get; set; }
        public ICollection<HoaDon>? HoaDons { get; set; }
        public ICollection<HangHetHan_TraVe>? HangTraVes { get; set; }
    }
}
