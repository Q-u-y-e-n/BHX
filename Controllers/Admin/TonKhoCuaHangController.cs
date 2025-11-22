using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TonKhoCuaHangController : Controller
    {
        private readonly BHXContext _context;

        public TonKhoCuaHangController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. XEM DANH SÁCH TỒN KHO TOÀN HỆ THỐNG
        // ============================================================
        public async Task<IActionResult> Index(int? cuaHangId, string status = "All", string searchString = "")
        {
            // Query cơ bản: Load kho kèm tên cửa hàng và tên sản phẩm
            var query = _context.TonKho_CuaHangs
                .Include(t => t.CuaHang)
                .Include(t => t.SanPham)
                .AsQueryable();

            // 1. Lọc theo Cửa hàng
            if (cuaHangId.HasValue)
            {
                query = query.Where(t => t.CuaHangID == cuaHangId);
            }

            // 2. Tìm kiếm tên sản phẩm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.SanPham.TenSanPham.Contains(searchString));
            }

            // 3. Lọc theo Trạng thái (Sắp hết / Tồn nhiều)
            if (status == "Low") // Sắp hết
            {
                query = query.Where(t => t.SoLuong <= 10);
            }
            else if (status == "High") // Tồn nhiều (ế)
            {
                query = query.Where(t => t.SoLuong >= 100);
            }
            else if (status == "Empty") // Hết hàng
            {
                query = query.Where(t => t.SoLuong == 0);
            }

            // Lấy danh sách cửa hàng để đổ vào Dropdown lọc
            ViewData["CuaHangID"] = new SelectList(_context.CuaHangs, "CuaHangID", "TenCuaHang", cuaHangId);

            // Lưu trạng thái lọc để giữ lại trên View
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = searchString;

            // Sắp xếp: Ưu tiên hàng sắp hết lên đầu để Admin chú ý
            var data = await query.OrderBy(t => t.SoLuong).ToListAsync();

            return View(data);
        }
    }
}