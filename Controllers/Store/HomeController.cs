using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")] // Chỉ tài khoản quyền Store mới được vào
    public class HomeController : Controller
    {
        private readonly BHXContext _context;

        public HomeController(BHXContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy CuaHangID từ Cookie (Claims)
            // Claim này đã được chúng ta thêm vào trong AccountController lúc Login
            var cuaHangIdClaim = User.FindFirst("CuaHangID")?.Value;

            if (string.IsNullOrEmpty(cuaHangIdClaim))
            {
                // Trường hợp tài khoản Store nhưng chưa được liên kết với Cửa hàng nào trong Database
                return View("Error_NoStore"); // Cần tạo view thông báo lỗi này hoặc redirect
            }

            int cuaHangId = int.Parse(cuaHangIdClaim);

            // 2. Lấy thông tin chi tiết Cửa hàng
            var cuaHang = await _context.CuaHangs
                .FirstOrDefaultAsync(c => c.CuaHangID == cuaHangId);

            if (cuaHang == null)
            {
                return NotFound("Không tìm thấy thông tin cửa hàng.");
            }

            // 3. Tính toán số liệu thống kê cho Dashboard

            // A. Tổng số mặt hàng đang tồn trong kho của cửa hàng này
            var tongTonKho = await _context.TonKho_CuaHangs
                .Where(t => t.CuaHangID == cuaHangId)
                .SumAsync(t => t.SoLuong);

            // B. Số phiếu đề xuất nhập hàng đang chờ Tổng công ty duyệt
            var deXuatDangCho = await _context.DeXuatNhapHangs
                .Where(d => d.CuaHangID == cuaHangId && d.TrangThai == "Chờ duyệt")
                .CountAsync();

            // C. Doanh thu hôm nay (Dựa vào Hóa đơn bán tại quầy)
            var doanhThuHomNay = await _context.HoaDons
                .Where(h => h.CuaHangID == cuaHangId && h.NgayLap.Date == DateTime.Now.Date)
                .SumAsync(h => (decimal?)h.TongTien) ?? 0; // (decimal?) để tránh lỗi nếu null

            // 4. Truyền dữ liệu qua View
            ViewBag.TongTonKho = tongTonKho;
            ViewBag.DeXuatCho = deXuatDangCho;
            ViewBag.DoanhThuHomNay = doanhThuHomNay;

            return View(cuaHang); // Truyền model CuaHang sang View
        }
    }
}