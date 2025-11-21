using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HangTraVeController : Controller
    {
        private readonly BHXContext _context;

        public HangTraVeController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DANH SÁCH HÀNG TRẢ VỀ (KÈM THỐNG KÊ THIỆT HẠI)
        // ============================================================
        public async Task<IActionResult> Index(string status = "Chờ duyệt")
        {
            var query = _context.DanhSachTraHangs
                .Include(t => t.CuaHang)
                .Include(t => t.ChiTietTraHangs)
                .ThenInclude(ct => ct.SanPham)
                .AsQueryable();

            if (status != "All")
            {
                query = query.Where(t => t.TrangThai == status);
            }

            // Tính tổng giá trị thiệt hại của các phiếu "Đã hủy" (Hàng hết hạn)
            var totalDamage = await _context.ChiTietTraHangs
                .Include(ct => ct.DanhSachTraHang)
                .Include(ct => ct.SanPham)
                .Where(ct => ct.DanhSachTraHang.TrangThai == "Đã hủy")
                .SumAsync(ct => ct.SoLuong * ct.SanPham.GiaNhap);

            ViewBag.TotalDamage = totalDamage;
            ViewBag.CurrentStatus = status;

            return View(await query.OrderByDescending(t => t.NgayTra).ToListAsync());
        }

        // ============================================================
        // 2. CHI TIẾT & XỬ LÝ
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            var phieu = await _context.DanhSachTraHangs
                .Include(t => t.CuaHang)
                .Include(t => t.ChiTietTraHangs)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(m => m.TraHangID == id);

            if (phieu == null) return NotFound();

            // Tính ước tính giá trị lô hàng này
            decimal uocTinhGiaTri = 0;
            if (phieu.ChiTietTraHangs != null)
            {
                uocTinhGiaTri = phieu.ChiTietTraHangs.Sum(x => x.SoLuong * (x.SanPham?.GiaNhap ?? 0));
            }
            ViewBag.UocTinhGiaTri = uocTinhGiaTri;

            return View(phieu);
        }

        // ============================================================
        // 3. XÁC NHẬN NHẬP KHO LỖI (Giữ hàng lại kho tổng)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var phieu = await _context.DanhSachTraHangs.FindAsync(id);
            if (phieu != null && phieu.TrangThai == "Chờ duyệt")
            {
                // Logic: Có thể cộng vào một kho riêng gọi là "Kho Hàng Lỗi"
                // Ở đây ta chỉ cập nhật trạng thái đơn giản
                phieu.TrangThai = "Đã nhận";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xác nhận nhận hàng về kho.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ============================================================
        // 4. HỦY HÀNG (Tiêu hủy hàng hết hạn -> Ghi nhận thiệt hại)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Destroy(int id)
        {
            var phieu = await _context.DanhSachTraHangs.FindAsync(id);
            if (phieu != null && phieu.TrangThai == "Chờ duyệt")
            {
                // Chuyển trạng thái sang Đã hủy (Hàng này coi như mất trắng)
                phieu.TrangThai = "Đã hủy";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xác nhận tiêu hủy lô hàng hết hạn này.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}