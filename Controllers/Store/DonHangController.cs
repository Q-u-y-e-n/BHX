using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần để dùng .Include, .ToListAsync
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;               // <--- QUAN TRỌNG: Để nhận diện BHXContext (Sửa lỗi CS0246)
using BHX_Web.Models.Entities;    // <--- Để nhận diện DonHang, ChiTietDonHang

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")] // Chỉ nhân viên cửa hàng mới được xem
    public class DonHangController : Controller
    {
        private readonly BHXContext _context;

        public DonHangController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DANH SÁCH ĐƠN HÀNG ONLINE (INDEX)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách đơn hàng, sắp xếp đơn mới nhất lên đầu
            var list = await _context.DonHangs
                .Include(d => d.KhachHang) // Load thông tin khách để biết ai mua
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(list);
        }

        // ============================================================
        // 2. CHI TIẾT ĐƠN HÀNG (DETAILS) - ĐỂ SOẠN HÀNG
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.SanPham) // Load sản phẩm để biết gói cái gì
                .FirstOrDefaultAsync(m => m.DonHangID == id);

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // ============================================================
        // 3. CẬP NHẬT TRẠNG THÁI (VÍ DỤ: ĐÃ GIAO)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string trangThai)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang != null)
            {
                donHang.TrangThai = trangThai; // Ví dụ: "Đang giao", "Hoàn thành"
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật đơn hàng #{id} thành công!";
            }
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}