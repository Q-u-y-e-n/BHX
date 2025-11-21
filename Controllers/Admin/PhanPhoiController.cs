using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PhanPhoiController : Controller
    {
        private readonly BHXContext _context;

        public PhanPhoiController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DANH SÁCH PHIẾU PHÂN PHỐI (INDEX)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách kèm thông tin Cửa hàng
            var list = await _context.PhieuPhanPhois
                .Include(p => p.CuaHang)
                .OrderByDescending(p => p.NgayTao)
                .ToListAsync();

            // Đảm bảo không bao giờ trả về Null sang View
            if (list == null) list = new List<PhieuPhanPhoi>();

            return View(list);
        }

        // ============================================================
        // 2. CHI TIẾT PHIẾU (DETAILS)
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phieu = await _context.PhieuPhanPhois
                .Include(p => p.CuaHang)
                .Include(p => p.ChiTietPhanPhois)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(m => m.PhieuPhanPhoiID == id);

            if (phieu == null) return NotFound();

            return View(phieu);
        }

        // ============================================================
        // 3. TẠO PHIẾU MỚI (GET)
        // ============================================================
        [HttpGet]
        public IActionResult Create()
        {
            // Load danh sách cửa hàng đang hoạt động
            ViewData["CuaHangID"] = new SelectList(_context.CuaHangs.Where(c => c.TrangThai == "Hoạt động"), "CuaHangID", "TenCuaHang");

            // Load danh sách sản phẩm kèm số lượng tồn kho để hiển thị
            var listSP = _context.KhoTongs
                .Include(k => k.SanPham)
                .Where(k => k.SanPham != null) // Tránh lỗi null
                .Select(k => new
                {
                    k.SanPhamID,
                    TenHienThi = k.SanPham.TenSanPham + " (Tồn: " + k.SoLuong + " " + k.SanPham.DonViTinh + ")",
                    TonKho = k.SoLuong
                })
                .ToList();

            ViewBag.ListSanPham = listSP;
            return View(new PhanPhoiViewModel());
        }

        // ============================================================
        // 3. TẠO PHIẾU MỚI (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhanPhoiViewModel model)
        {
            if (model.ChiTiets == null || !model.ChiTiets.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một sản phẩm.");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // A. Kiểm tra tồn kho trước
                        foreach (var item in model.ChiTiets)
                        {
                            var khoTong = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);
                            if (khoTong == null || khoTong.SoLuong < item.SoLuong)
                            {
                                throw new Exception($"Sản phẩm ID {item.SanPhamID} không đủ hàng trong kho tổng! (Yêu cầu: {item.SoLuong}, Tồn: {khoTong?.SoLuong ?? 0})");
                            }
                        }

                        // B. Tạo Phiếu
                        var phieu = new PhieuPhanPhoi
                        {
                            CuaHangID = model.CuaHangID,
                            NgayTao = model.NgayTao,
                            TrangThai = "Đang giao" // Mặc định là đang giao
                        };
                        _context.PhieuPhanPhois.Add(phieu);
                        await _context.SaveChangesAsync();

                        // C. Lưu Chi tiết & Trừ Kho Tổng
                        foreach (var item in model.ChiTiets)
                        {
                            _context.ChiTietPhanPhois.Add(new ChiTietPhanPhoi
                            {
                                PhieuPhanPhoiID = phieu.PhieuPhanPhoiID,
                                SanPhamID = item.SanPhamID,
                                SoLuong = item.SoLuong
                            });

                            // Trừ kho
                            var khoTong = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);
                            if (khoTong != null)
                            {
                                khoTong.SoLuong -= item.SoLuong;
                                _context.Update(khoTong);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Phân phối thành công! Đã trừ kho tổng.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }

            // Reload dữ liệu nếu lỗi
            ViewData["CuaHangID"] = new SelectList(_context.CuaHangs.Where(c => c.TrangThai == "Hoạt động"), "CuaHangID", "TenCuaHang", model.CuaHangID);
            var listSPLoad = _context.KhoTongs.Include(k => k.SanPham).Where(k => k.SanPham != null)
                .Select(k => new { k.SanPhamID, TenHienThi = k.SanPham.TenSanPham + " (Tồn: " + k.SoLuong + ")", TonKho = k.SoLuong }).ToList();
            ViewBag.ListSanPham = listSPLoad;

            return View(model);
        }

        // ============================================================
        // 4. XÓA PHIẾU (POST)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var phieu = await _context.PhieuPhanPhois
                .Include(p => p.ChiTietPhanPhois)
                .FirstOrDefaultAsync(p => p.PhieuPhanPhoiID == id);

            if (phieu == null) return NotFound();

            // Chỉ cho xóa nếu hàng chưa đến nơi
            if (phieu.TrangThai == "Đã nhận")
            {
                TempData["ErrorMessage"] = "Không thể hủy phiếu đã được cửa hàng nhận!";
                return RedirectToAction(nameof(Index));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Hoàn lại số lượng vào Kho Tổng
                    if (phieu.ChiTietPhanPhois != null)
                    {
                        foreach (var item in phieu.ChiTietPhanPhois)
                        {
                            var kho = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);
                            if (kho != null)
                            {
                                kho.SoLuong += item.SoLuong; // Cộng lại
                                _context.Update(kho);
                            }
                        }
                    }

                    _context.PhieuPhanPhois.Remove(phieu);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã hủy phân phối và hoàn trả hàng về kho.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}