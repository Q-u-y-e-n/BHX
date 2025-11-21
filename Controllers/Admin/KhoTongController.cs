using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class KhoTongController : Controller
    {
        private readonly BHXContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public KhoTongController(BHXContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // =========================================================
        // 1. DANH SÁCH TỒN KHO (INDEX)
        // =========================================================
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.KhoTongs.Include(k => k.SanPham).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // FIX LỖI CS8602: Kiểm tra k.SanPham != null trước khi truy cập
                query = query.Where(k => k.SanPham != null &&
                                       ((k.SanPham.TenSanPham != null && k.SanPham.TenSanPham.Contains(searchString))
                                     || (k.SanPham.LoaiSanPham != null && k.SanPham.LoaiSanPham.Contains(searchString))));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await query.OrderByDescending(k => k.NgayCapNhat).ToListAsync());
        }

        // =========================================================
        // 2. THÊM MỚI (CREATE)
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SanPhamViewModel { SoLuong = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPhamViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? imagePath = null;

                if (model.HinhAnhFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.HinhAnhFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.HinhAnhFile.CopyToAsync(fileStream);
                    }
                    imagePath = "/images/products/" + uniqueFileName;
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var sanPham = new SanPham
                        {
                            TenSanPham = model.TenSanPham,
                            DonViTinh = model.DonViTinh,
                            GiaNhap = model.GiaNhap,
                            GiaBan = model.GiaBan,
                            LoaiSanPham = model.LoaiSanPham,
                            HinhAnh = imagePath
                        };
                        _context.SanPhams.Add(sanPham);
                        await _context.SaveChangesAsync();

                        var khoTong = new KhoTong
                        {
                            SanPhamID = sanPham.SanPhamID,
                            SoLuong = model.SoLuong,
                            NgayCapNhat = DateTime.Now
                        };
                        _context.KhoTongs.Add(khoTong);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }
            return View(model);
        }

        // =========================================================
        // 3. CHỈNH SỬA (EDIT)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var khoTong = await _context.KhoTongs.Include(k => k.SanPham).FirstOrDefaultAsync(k => k.KhoTongID == id);

            // Kiểm tra kỹ null ở đây
            if (khoTong == null || khoTong.SanPham == null) return NotFound();

            // FIX LỖI CS8601: Dùng toán tử ?? "" để đảm bảo không gán null vào chuỗi không được null
            var model = new SanPhamViewModel
            {
                SanPhamID = khoTong.SanPham.SanPhamID,
                TenSanPham = khoTong.SanPham.TenSanPham ?? "", // Nếu null thì lấy chuỗi rỗng
                DonViTinh = khoTong.SanPham.DonViTinh ?? "",
                GiaNhap = khoTong.SanPham.GiaNhap,
                GiaBan = khoTong.SanPham.GiaBan,
                LoaiSanPham = khoTong.SanPham.LoaiSanPham,
                HinhAnhHienTai = khoTong.SanPham.HinhAnh,
                SoLuong = khoTong.SoLuong
            };

            ViewBag.KhoTongID = khoTong.KhoTongID;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SanPhamViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sanPham = await _context.SanPhams.FindAsync(model.SanPhamID);
                var khoTong = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == model.SanPhamID);

                if (sanPham == null || khoTong == null) return NotFound();

                if (model.HinhAnhFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.HinhAnhFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.HinhAnhFile.CopyToAsync(fileStream);
                    }

                    // Xóa ảnh cũ để tránh rác
                    if (!string.IsNullOrEmpty(sanPham.HinhAnh))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, sanPham.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    sanPham.HinhAnh = "/images/products/" + uniqueFileName;
                }

                sanPham.TenSanPham = model.TenSanPham;
                sanPham.DonViTinh = model.DonViTinh;
                sanPham.GiaNhap = model.GiaNhap;
                sanPham.GiaBan = model.GiaBan;
                sanPham.LoaiSanPham = model.LoaiSanPham;

                khoTong.SoLuong = model.SoLuong;
                khoTong.NgayCapNhat = DateTime.Now;

                try
                {
                    _context.Update(sanPham);
                    _context.Update(khoTong);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                }
            }
            return View(model);
        }

        // =========================================================
        // 4. XÓA (DELETE)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var khoTong = await _context.KhoTongs.FindAsync(id);
            if (khoTong == null) return NotFound();

            var sanPham = await _context.SanPhams.FindAsync(khoTong.SanPhamID);

            try
            {
                _context.KhoTongs.Remove(khoTong);
                if (sanPham != null)
                {
                    if (!string.IsNullOrEmpty(sanPham.HinhAnh))
                    {
                        string path = Path.Combine(_webHostEnvironment.WebRootPath, sanPham.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }
                    _context.SanPhams.Remove(sanPham);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa sản phẩm.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Không thể xóa do sản phẩm đã có giao dịch.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}