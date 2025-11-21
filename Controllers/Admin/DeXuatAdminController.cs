using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DeXuatAdminController : Controller
    {
        private readonly BHXContext _context;

        public DeXuatAdminController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DANH SÁCH YÊU CẦU (INDEX)
        // ============================================================
        public async Task<IActionResult> Index(string status = "Chờ duyệt")
        {
            var query = _context.DeXuatNhapHangs
                .Include(d => d.CuaHang)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(d => d.TrangThai == status);
            }

            ViewBag.CurrentStatus = status;
            return View(await query.OrderByDescending(d => d.NgayDeXuat).ToListAsync());
        }

        // ============================================================
        // 2. XEM CHI TIẾT ĐỂ DUYỆT
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            var phieu = await _context.DeXuatNhapHangs
                .Include(d => d.CuaHang)
                .Include(d => d.ChiTietDeXuatNhaps)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DeXuatID == id);

            if (phieu == null) return NotFound();

            // Lấy thêm thông tin Tồn Kho Tổng hiện tại để Admin biết có đủ hàng duyệt không
            foreach (var item in phieu.ChiTietDeXuatNhaps)
            {
                // Tạm lưu tồn kho tổng vào ViewData hoặc ViewBag để hiển thị
                var tonKhoTong = await _context.KhoTongs
                    .Where(k => k.SanPhamID == item.SanPhamID)
                    .Select(k => k.SoLuong)
                    .FirstOrDefaultAsync();

                // Dùng ViewData dynamic để truyền sang View
                ViewData[$"TonKho_{item.SanPhamID}"] = tonKhoTong;
            }

            return View(phieu);
        }

        // ============================================================
        // 3. XỬ LÝ DUYỆT PHIẾU (APPROVE) -> TỰ TẠO PHIẾU PHÂN PHỐI
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var deXuat = await _context.DeXuatNhapHangs
                .Include(d => d.ChiTietDeXuatNhaps)
                .FirstOrDefaultAsync(d => d.DeXuatID == id);

            if (deXuat == null) return NotFound();
            if (deXuat.TrangThai != "Chờ duyệt") return RedirectToAction(nameof(Index));

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tạo Phiếu Phân Phối mới (Tự động)
                    var phieuPhanPhoi = new PhieuPhanPhoi
                    {
                        CuaHangID = deXuat.CuaHangID,
                        NgayTao = DateTime.Now,
                        TrangThai = "Đang giao"
                    };
                    _context.PhieuPhanPhois.Add(phieuPhanPhoi);
                    await _context.SaveChangesAsync();

                    // 2. Duyệt từng sản phẩm để trừ kho và tạo chi tiết phân phối
                    foreach (var item in deXuat.ChiTietDeXuatNhaps)
                    {
                        // Kiểm tra kho tổng
                        var khoTong = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);

                        // Nếu kho không đủ hàng -> Chỉ xuất số lượng tối đa đang có
                        int soLuongXuat = item.SoLuong;
                        if (khoTong == null || khoTong.SoLuong < item.SoLuong)
                        {
                            // Logic: Có thể báo lỗi hoặc chỉ xuất phần còn lại. 
                            // Ở đây tôi chọn cách: Báo lỗi để Admin nhập hàng trước.
                            throw new Exception($"Sản phẩm ID {item.SanPhamID} không đủ hàng trong Kho Tổng để duyệt!");
                        }

                        // Trừ kho tổng
                        khoTong.SoLuong -= soLuongXuat;
                        _context.Update(khoTong);

                        // Tạo chi tiết phân phối
                        _context.ChiTietPhanPhois.Add(new ChiTietPhanPhoi
                        {
                            PhieuPhanPhoiID = phieuPhanPhoi.PhieuPhanPhoiID,
                            SanPhamID = item.SanPhamID,
                            SoLuong = soLuongXuat
                        });
                    }

                    // 3. Cập nhật trạng thái Đề Xuất
                    deXuat.TrangThai = "Đã duyệt";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Đã duyệt đề xuất #{id} và tạo Phiếu Phân Phối tự động!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi khi duyệt: " + ex.Message;
                    return RedirectToAction(nameof(Details), new { id = id });
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 4. TỪ CHỐI (REJECT)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var deXuat = await _context.DeXuatNhapHangs.FindAsync(id);
            if (deXuat != null && deXuat.TrangThai == "Chờ duyệt")
            {
                deXuat.TrangThai = "Từ chối";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã từ chối yêu cầu nhập hàng.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}