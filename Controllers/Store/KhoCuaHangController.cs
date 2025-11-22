using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using System.Security.Claims;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")] // Chỉ nhân viên cửa hàng mới được truy cập
    public class KhoCuaHangController : Controller
    {
        private readonly BHXContext _context;

        public KhoCuaHangController(BHXContext context)
        {
            _context = context;
        }

        // Helper: Lấy ID cửa hàng từ User đang đăng nhập
        private int GetCurrentStoreId()
        {
            var claim = User.FindFirst("CuaHangID");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // 1. DANH SÁCH TỒN KHO (INDEX)
        public async Task<IActionResult> Index(string searchString)
        {
            int storeId = GetCurrentStoreId();
            if (storeId == 0) return RedirectToAction("Login", "Account", new { area = "" });

            // Lấy danh sách tồn kho của cửa hàng này, kèm thông tin sản phẩm
            var tonKho = _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm kiếm theo tên sản phẩm
                tonKho = tonKho.Where(t => t.SanPham.TenSanPham.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await tonKho.OrderBy(t => t.SoLuong).ToListAsync()); // Sắp xếp tăng dần theo số lượng (để thấy hàng sắp hết trước)
        }

        // 2. BÁO CÁO TỒN KHO (Gửi thông báo/email hoặc lưu log)
        // Trong mô hình này, Tổng công ty có thể xem trực tiếp DB nên chức năng này mang tính chất "Chốt sổ" hoặc "Thông báo khẩn"
        [HttpPost]
        public IActionResult BaoCaoTonKho()
        {
            // Logic: Có thể lưu vào bảng Lịch sử báo cáo hoặc gửi Email cho Admin
            // Ở đây mình giả lập là gửi thành công
            TempData["SuccessMessage"] = "Đã gửi báo cáo tồn kho về Tổng công ty thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}