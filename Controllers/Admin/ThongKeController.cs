using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels; // Tạo ViewModel mới nếu cần, hoặc dùng ViewBag

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ThongKeController : Controller
    {
        private readonly BHXContext _context;

        public ThongKeController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DASHBOARD TỔNG HỢP DOANH THU
        // ============================================================
        public async Task<IActionResult> Index(int? thang, int? nam)
        {
            int t = thang ?? DateTime.Now.Month;
            int n = nam ?? DateTime.Now.Year;

            // A. Lấy dữ liệu bán hàng tổng hợp của tháng này
            var baoCaoData = await _context.BanHang_TongHops
                .Include(b => b.CuaHang)
                .Where(b => b.Thang == t && b.Nam == n)
                .ToListAsync();

            // B. Lấy dữ liệu hàng lỗi/trả về (Thất thoát)
            // (Logic: Cộng tổng giá trị các phiếu trả có trạng thái "Đã hủy" trong tháng này)
            var thatThoatData = await _context.ChiTietTraHangs
                .Include(ct => ct.DanhSachTraHang)
                .Include(ct => ct.SanPham)
                .Where(ct => ct.DanhSachTraHang.TrangThai == "Đã hủy"
                          && ct.DanhSachTraHang.NgayTra.Month == t
                          && ct.DanhSachTraHang.NgayTra.Year == n)
                .Select(ct => new
                {
                    CuaHangID = ct.DanhSachTraHang.CuaHangID,
                    GiaTri = ct.SoLuong * (ct.SanPham.GiaNhap)
                })
                .ToListAsync();

            // C. Tính toán số liệu tổng quan toàn hệ thống
            ViewBag.TongDoanhThuHeThong = baoCaoData.Sum(x => x.DoanhThu);
            ViewBag.TongThatThoatHeThong = thatThoatData.Sum(x => x.GiaTri);
            ViewBag.LoiNhuanUocTinh = ViewBag.TongDoanhThuHeThong - ViewBag.TongThatThoatHeThong;

            // D. Gom nhóm dữ liệu theo Cửa hàng để hiển thị danh sách
            var danhSachCuaHang = _context.CuaHangs.ToList(); // Lấy tất cả cửa hàng

            var reportList = new List<dynamic>();

            foreach (var store in danhSachCuaHang)
            {
                decimal doanhThu = baoCaoData.Where(x => x.CuaHangID == store.CuaHangID).Sum(x => x.DoanhThu);
                decimal thatThoat = thatThoatData.Where(x => x.CuaHangID == store.CuaHangID).Sum(x => x.GiaTri);

                reportList.Add(new
                {
                    CuaHangID = store.CuaHangID,
                    TenCuaHang = store.TenCuaHang,
                    DoanhThu = doanhThu,
                    ThatThoat = thatThoat,
                    LoiNhuan = doanhThu - thatThoat,
                    TrangThaiBaoCao = doanhThu > 0 ? "Đã báo cáo" : "Chưa báo cáo"
                });
            }

            // Truyền dữ liệu sang View
            ViewBag.Thang = t;
            ViewBag.Nam = n;
            ViewBag.ReportList = reportList;

            return View();
        }

        // ============================================================
        // 2. CHI TIẾT DOANH THU CỦA MỘT CỬA HÀNG
        // ============================================================
        public async Task<IActionResult> Details(int id, int thang, int nam)
        {
            var cuaHang = await _context.CuaHangs.FindAsync(id);
            if (cuaHang == null) return NotFound();

            // Lấy chi tiết sản phẩm bán ra
            var chiTietBan = await _context.BanHang_TongHops
                .Include(b => b.SanPham)
                .Where(b => b.CuaHangID == id && b.Thang == thang && b.Nam == nam)
                .OrderByDescending(b => b.DoanhThu)
                .ToListAsync();

            // Lấy chi tiết hàng thất thoát
            var chiTietLoi = await _context.ChiTietTraHangs
                .Include(ct => ct.SanPham)
                .Where(ct => ct.DanhSachTraHang.CuaHangID == id
                          && ct.DanhSachTraHang.TrangThai == "Đã hủy"
                          && ct.DanhSachTraHang.NgayTra.Month == thang
                          && ct.DanhSachTraHang.NgayTra.Year == nam)
                .Select(ct => new
                {
                    TenSP = ct.SanPham.TenSanPham,
                    SoLuong = ct.SoLuong,
                    GiaTri = ct.SoLuong * ct.SanPham.GiaNhap,
                    LyDo = ct.LyDo
                })
                .ToListAsync();

            ViewBag.CuaHang = cuaHang;
            ViewBag.Thang = thang;
            ViewBag.Nam = nam;
            ViewBag.ChiTietLoi = chiTietLoi;
            ViewBag.TongDoanhThu = chiTietBan.Sum(x => x.DoanhThu);
            ViewBag.TongThatThoat = chiTietLoi.Sum(x => x.GiaTri);

            return View(chiTietBan);
        }
    }
}