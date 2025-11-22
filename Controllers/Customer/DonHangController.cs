using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using System.Security.Claims;

namespace BHX_Web.Controllers.Customer
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")] // Chỉ Khách hàng đã đăng nhập mới xem được
    public class DonHangController : Controller
    {
        private readonly BHXContext _context;

        public DonHangController(BHXContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐƠN HÀNG CỦA TÔI
        public async Task<IActionResult> Index()
        {
            // Lấy UserID hiện tại
            var userIdStr = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            // Tìm Khách hàng tương ứng với UserID này (qua số điện thoại username)
            // Lưu ý: Logic này phụ thuộc vào việc bạn lưu SĐT làm Username
            var userPhone = User.Identity?.Name;

            var listDonHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Where(d => d.KhachHang.SoDienThoai == userPhone) // Lọc theo SĐT của user đang login
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(listDonHang);
        }

        // 2. CHI TIẾT ĐƠN HÀNG
        public async Task<IActionResult> Details(int id)
        {
            // Lấy thông tin đơn hàng kèm chi tiết sản phẩm
            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DonHangID == id);

            if (donHang == null) return NotFound();

            // Kiểm tra bảo mật: Khách chỉ được xem đơn của mình
            var userPhone = User.Identity?.Name;
            var khachHang = await _context.KhachHangs.FindAsync(donHang.KhachHangID);

            if (khachHang == null || khachHang.SoDienThoai != userPhone)
            {
                return Unauthorized("Bạn không có quyền xem đơn hàng này.");
            }

            return View(donHang);
        }
    }
}