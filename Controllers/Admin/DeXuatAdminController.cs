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
        // 1. DANH SÁCH YÊU CẦU
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
        // 2. XEM CHI TIẾT & KIỂM TRA TỒN KHO
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            var phieu = await _context.DeXuatNhapHangs
                .Include(d => d.CuaHang)
                .Include(d => d.ChiTietDeXuatNhaps)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DeXuatID == id);

            if (phieu == null) return NotFound();

            // Lấy thông tin Tồn Kho Tổng hiện tại để hiển thị lên View
            // Giúp Admin biết có đủ hàng để duyệt hay không
            foreach (var item in phieu.ChiTietDeXuatNhaps)
            {
                // Tìm dòng tồn kho của sản phẩm này
                var khoTong = await _context.KhoTongs
                    .Where(k => k.SanPhamID == item.SanPhamID)
                    .OrderByDescending(k => k.SoLuong) // Lấy dòng có số lượng lớn nhất nếu lỡ bị trùng
                    .FirstOrDefaultAsync();

                int soLuongTon = khoTong?.SoLuong ?? 0;

                // Lưu vào ViewData để View sử dụng
                ViewData[$"TonKho_{item.SanPhamID}"] = soLuongTon;
            }

            return View(phieu);
        }

        // ============================================================
        // 3. XỬ LÝ DUYỆT (APPROVE) - LOGIC QUAN TRỌNG
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            // 1. Lấy dữ liệu phiếu đề xuất
            var deXuat = await _context.DeXuatNhapHangs
                .Include(d => d.ChiTietDeXuatNhaps)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DeXuatID == id);

            if (deXuat == null) return NotFound();

            // Chỉ được duyệt phiếu đang chờ
            if (deXuat.TrangThai != "Chờ duyệt")
            {
                TempData["ErrorMessage"] = "Phiếu này đã được xử lý rồi!";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 2. Kiểm tra tồn kho TẤT CẢ sản phẩm trước khi thực hiện
                    // Nếu thiếu dù chỉ 1 món cũng không cho duyệt (để đảm bảo tính trọn vẹn)
                    foreach (var item in deXuat.ChiTietDeXuatNhaps)
                    {
                        var khoTong = await _context.KhoTongs
                             .FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);

                        int tonHienTai = khoTong?.SoLuong ?? 0;

                        if (tonHienTai < item.SoLuong)
                        {
                            throw new Exception($"Thiếu hàng: {item.SanPham?.TenSanPham} (Cần: {item.SoLuong}, Kho chỉ còn: {tonHienTai}). Vui lòng nhập thêm hàng!");
                        }
                    }

                    // 3. Tạo Phiếu Phân Phối (Header)
                    var phieuPhanPhoi = new PhieuPhanPhoi
                    {
                        CuaHangID = deXuat.CuaHangID,
                        NgayTao = DateTime.Now,
                        TrangThai = "Đang giao"
                    };
                    _context.PhieuPhanPhois.Add(phieuPhanPhoi);
                    await _context.SaveChangesAsync(); // Lưu để lấy ID

                    // 4. Trừ Kho & Tạo Chi Tiết Phân Phối
                    foreach (var item in deXuat.ChiTietDeXuatNhaps)
                    {
                        // Trừ kho tổng
                        var khoTong = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);
                        if (khoTong != null)
                        {
                            khoTong.SoLuong -= item.SoLuong;
                            _context.Update(khoTong);
                        }

                        // Tạo dòng chi tiết phân phối
                        _context.ChiTietPhanPhois.Add(new ChiTietPhanPhoi
                        {
                            PhieuPhanPhoiID = phieuPhanPhoi.PhieuPhanPhoiID,
                            SanPhamID = item.SanPhamID,
                            SoLuong = item.SoLuong
                        });
                    }

                    // 5. Cập nhật trạng thái Đề Xuất
                    deXuat.TrangThai = "Đã duyệt";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Duyệt thành công! Đã tạo phiếu xuất kho #{phieuPhanPhoi.PhieuPhanPhoiID}.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = ex.Message; // Hiển thị lỗi cụ thể
                    return RedirectToAction(nameof(Details), new { id = id });
                }
            }
        }

        // ============================================================
        // 4. TỪ CHỐI (REJECT)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var deXuat = await _context.DeXuatNhapHangs.FindAsync(id);
            if (deXuat != null && deXuat.TrangThai == "Chờ duyệt")
            {
                deXuat.TrangThai = "Từ chối";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã từ chối yêu cầu.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}