using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")] // Chỉ tài khoản Cửa Hàng mới được vào
    public class NhanHangController : Controller
    {
        private readonly BHXContext _context;

        public NhanHangController(BHXContext context)
        {
            _context = context;
        }

        // Helper: Lấy ID Cửa hàng hiện tại từ Cookie
        private int GetCurrentStoreId()
        {
            var claim = User.FindFirst("CuaHangID");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // ============================================================
        // 1. DANH SÁCH PHIẾU CHỜ NHẬN (INDEX)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            int storeId = GetCurrentStoreId();
            if (storeId == 0) return RedirectToAction("Login", "Account", new { area = "" });

            // Lấy các phiếu phân phối gửi đến cửa hàng này
            var list = await _context.PhieuPhanPhois
                .Where(p => p.CuaHangID == storeId) // Chỉ lấy của shop mình
                .OrderByDescending(p => p.NgayTao)
                .ToListAsync();

            return View(list);
        }

        // ============================================================
        // 2. CHI TIẾT PHIẾU (ĐỂ KIỂM TRA TRƯỚC KHI NHẬN)
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            int storeId = GetCurrentStoreId();

            var phieu = await _context.PhieuPhanPhois
                .Include(p => p.ChiTietPhanPhois)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(p => p.PhieuPhanPhoiID == id && p.CuaHangID == storeId);

            if (phieu == null) return NotFound();

            return View(phieu);
        }

        // ============================================================
        // 3. XÁC NHẬN ĐÃ NHẬN HÀNG (ACTION QUAN TRỌNG NHẤT)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            int storeId = GetCurrentStoreId();

            // Tìm phiếu
            var phieu = await _context.PhieuPhanPhois
                .Include(p => p.ChiTietPhanPhois)
                .FirstOrDefaultAsync(p => p.PhieuPhanPhoiID == id && p.CuaHangID == storeId);

            if (phieu == null) return NotFound();

            // Kiểm tra trạng thái: Chỉ xử lý nếu đang là "Đang giao"
            if (phieu.TrangThai == "Đã nhận")
            {
                TempData["ErrorMessage"] = "Phiếu này đã được nhập kho trước đó rồi!";
                return RedirectToAction(nameof(Index));
            }

            // BẮT ĐẦU TRANSACTION
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // A. Cập nhật trạng thái phiếu
                    phieu.TrangThai = "Đã nhận";
                    _context.Update(phieu);

                    // B. Cộng tồn kho Cửa Hàng
                    foreach (var item in phieu.ChiTietPhanPhois)
                    {
                        // Tìm xem sản phẩm này đã có trong kho cửa hàng chưa
                        var tonKhoShop = await _context.TonKho_CuaHangs
                            .FirstOrDefaultAsync(t => t.CuaHangID == storeId && t.SanPhamID == item.SanPhamID);

                        if (tonKhoShop != null)
                        {
                            // Nếu có rồi -> Cộng dồn
                            tonKhoShop.SoLuong += item.SoLuong;
                            _context.Update(tonKhoShop);
                        }
                        else
                        {
                            // Nếu chưa có -> Tạo mới dòng tồn kho
                            _context.TonKho_CuaHangs.Add(new TonKho_CuaHang
                            {
                                CuaHangID = storeId,
                                SanPhamID = item.SanPhamID,
                                SoLuong = item.SoLuong
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã xác nhận nhận hàng! Kho cửa hàng đã được cập nhật.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}