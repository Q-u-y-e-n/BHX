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
    public class PhieuNhapController : Controller
    {
        private readonly BHXContext _context;

        public PhieuNhapController(BHXContext context)
        {
            _context = context;
        }

        // 1. INDEX
        public async Task<IActionResult> Index()
        {
            var list = await _context.PhieuNhap_Tongs
                .Include(p => p.NhaCungCap)
                .OrderByDescending(p => p.NgayNhap)
                .ToListAsync();
            return View(list);
        }

        // 2. DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var phieu = await _context.PhieuNhap_Tongs
                .Include(p => p.NhaCungCap)
                .Include(p => p.ChiTietNhapTongs)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(m => m.PhieuNhapID == id);
            if (phieu == null) return NotFound();
            return View(phieu);
        }

        // ==================================================
        // 3. CREATE (GET) - CHỈ CÓ 1 HÀM NÀY THÔI
        // ==================================================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.ListNCC = new SelectList(_context.NhaCungCaps, "NhaCungCapID", "TenNCC");
            ViewBag.ListSanPham = _context.SanPhams.Select(s => new { s.SanPhamID, s.TenSanPham }).ToList();
            return View(new PhieuNhapViewModel());
        }

        // ==================================================
        // 3. CREATE (POST) - CHỈ CÓ 1 HÀM NÀY THÔI
        // ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuNhapViewModel model)
        {
            if (model.ChiTiets == null || !model.ChiTiets.Any())
                ModelState.AddModelError("", "Vui lòng nhập ít nhất một dòng sản phẩm.");

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var safeChiTiets = model.ChiTiets ?? new List<ChiTietNhapItem>();
                        var nhomHangTheoNCC = safeChiTiets.GroupBy(x => x.NhaCungCapID);
                        int count = 0;

                        foreach (var group in nhomHangTheoNCC)
                        {
                            var phieuNhap = new PhieuNhap_Tong
                            {
                                NhaCungCapID = group.Key,
                                NgayNhap = model.NgayNhap,
                                TongTien = group.Sum(x => x.SoLuong * x.DonGia)
                            };
                            _context.PhieuNhap_Tongs.Add(phieuNhap);
                            await _context.SaveChangesAsync();

                            foreach (var item in group)
                            {
                                var chiTiet = new ChiTietNhap_Tong
                                {
                                    PhieuNhapID = phieuNhap.PhieuNhapID,
                                    SanPhamID = item.SanPhamID,
                                    SoLuong = item.SoLuong,
                                    DonGia = item.DonGia
                                };
                                _context.ChiTietNhap_Tongs.Add(chiTiet);

                                var kho = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);
                                if (kho != null)
                                {
                                    kho.SoLuong += item.SoLuong;
                                    kho.NgayCapNhat = DateTime.Now;
                                    _context.Update(kho);
                                }
                                else
                                {
                                    _context.KhoTongs.Add(new KhoTong { SanPhamID = item.SanPhamID, SoLuong = item.SoLuong, NgayCapNhat = DateTime.Now });
                                }

                                var sp = await _context.SanPhams.FindAsync(item.SanPhamID);
                                if (sp != null) { sp.GiaNhap = item.DonGia; _context.Update(sp); }
                            }
                            count++;
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        TempData["SuccessMessage"] = $"Đã tạo {count} phiếu nhập kho!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }
            ViewBag.ListNCC = new SelectList(_context.NhaCungCaps, "NhaCungCapID", "TenNCC");
            ViewBag.ListSanPham = _context.SanPhams.Select(s => new { s.SanPhamID, s.TenSanPham }).ToList();
            return View(model);
        }

        // 4. DELETE
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var phieu = await _context.PhieuNhap_Tongs.Include(p => p.ChiTietNhapTongs).FirstOrDefaultAsync(p => p.PhieuNhapID == id);
            if (phieu == null) return NotFound();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (phieu.ChiTietNhapTongs != null)
                    {
                        foreach (var item in phieu.ChiTietNhapTongs)
                        {
                            var kho = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == item.SanPhamID);
                            if (kho != null) { kho.SoLuong -= item.SoLuong; _context.Update(kho); }
                        }
                    }
                    _context.PhieuNhap_Tongs.Remove(phieu);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Đã hủy phiếu nhập.";
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