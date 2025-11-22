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
                // Tìm kiếm an toàn (tránh null)
                query = query.Where(k => (k.SanPham.TenSanPham != null && k.SanPham.TenSanPham.Contains(searchString))
                                      || (k.SanPham.LoaiSanPham != null && k.SanPham.LoaiSanPham.Contains(searchString)));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await query.OrderByDescending(k => k.NgayCapNhat).ToListAsync());
        }

        // =========================================================
        // 2. TẠO MỚI (GET)
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SanPhamViewModel { SoLuong = 0 });
        }

        // =========================================================
        // 3. TẠO MỚI (POST)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPhamViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? imagePath = null;

                // 1. Xử lý Upload Ảnh
                if (model.HinhAnhFile != null)
                {
                    // Lưu vào thư mục wwwroot/images/products để gọn gàng
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                    // Tự động tạo thư mục nếu chưa có
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // Tạo tên file độc nhất
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.HinhAnhFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Dùng 'using' để đóng file ngay sau khi copy xong (Tránh lỗi file đang được sử dụng)
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.HinhAnhFile.CopyToAsync(fileStream);
                    }

                    imagePath = "/images/products/" + uniqueFileName;
                }

                // 2. Transaction lưu Database
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

                        // Xóa ảnh rác nếu lưu DB thất bại
                        if (imagePath != null)
                        {
                            try
                            {
                                string physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                                if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
                            }
                            catch { /* Bỏ qua lỗi xóa file */ }
                        }
                    }
                }
            }
            return View(model);
        }

        // =========================================================
        // 4. CHỈNH SỬA (GET)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var khoTong = await _context.KhoTongs.Include(k => k.SanPham).FirstOrDefaultAsync(k => k.KhoTongID == id);
            if (khoTong == null || khoTong.SanPham == null) return NotFound();

            var model = new SanPhamViewModel
            {
                SanPhamID = khoTong.SanPham.SanPhamID,
                TenSanPham = khoTong.SanPham.TenSanPham,
                DonViTinh = khoTong.SanPham.DonViTinh,
                GiaNhap = khoTong.SanPham.GiaNhap,
                GiaBan = khoTong.SanPham.GiaBan,
                LoaiSanPham = khoTong.SanPham.LoaiSanPham,
                HinhAnhHienTai = khoTong.SanPham.HinhAnh,
                SoLuong = khoTong.SoLuong
            };

            ViewBag.KhoTongID = khoTong.KhoTongID;
            return View(model);
        }

        // =========================================================
        // 5. CHỈNH SỬA (POST) - FIX LỖI IO EXCEPTION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SanPhamViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sanPham = await _context.SanPhams.FindAsync(model.SanPhamID);
                var khoTong = await _context.KhoTongs.FirstOrDefaultAsync(k => k.SanPhamID == model.SanPhamID);

                if (sanPham == null || khoTong == null) return NotFound();

                // 1. Xử lý ảnh mới (nếu có upload)
                if (model.HinhAnhFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.HinhAnhFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file mới
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.HinhAnhFile.CopyToAsync(fileStream);
                    }

                    // [FIX LỖI QUAN TRỌNG]: Xóa ảnh cũ an toàn
                    if (!string.IsNullOrEmpty(sanPham.HinhAnh))
                    {
                        try
                        {
                            // Lấy đường dẫn vật lý cũ
                            string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, sanPham.HinhAnh.TrimStart('/', '\\'));

                            // Kiểm tra file có tồn tại không
                            if (System.IO.File.Exists(oldPath))
                            {
                                // Cố gắng xóa
                                System.IO.File.Delete(oldPath);
                            }
                        }
                        catch (IOException)
                        {
                            // NẾU FILE ĐANG BỊ KHÓA: Bỏ qua việc xóa, không làm sập web.
                            // File cũ sẽ thành file rác, ta có thể dọn dẹp thủ công sau.
                            Console.WriteLine("File đang được sử dụng, không thể xóa: " + sanPham.HinhAnh);
                        }
                        catch (Exception)
                        {
                            // Bỏ qua các lỗi khác
                        }
                    }

                    // Cập nhật đường dẫn ảnh mới
                    sanPham.HinhAnh = "/images/products/" + uniqueFileName;
                }

                // 2. Cập nhật thông tin
                sanPham.TenSanPham = model.TenSanPham;
                sanPham.DonViTinh = model.DonViTinh;
                sanPham.GiaNhap = model.GiaNhap;
                sanPham.GiaBan = model.GiaBan;
                sanPham.LoaiSanPham = model.LoaiSanPham;

                // 3. Cập nhật kho
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
        // 6. XÓA (DELETE)
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
                    // Xóa ảnh vật lý an toàn
                    if (!string.IsNullOrEmpty(sanPham.HinhAnh))
                    {
                        try
                        {
                            string path = Path.Combine(_webHostEnvironment.WebRootPath, sanPham.HinhAnh.TrimStart('/', '\\'));
                            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                        }
                        catch { /* Bỏ qua lỗi nếu không xóa được file */ }
                    }
                    _context.SanPhams.Remove(sanPham);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa sản phẩm.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Không thể xóa do sản phẩm đã có giao dịch. Hãy chỉnh số lượng về 0.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}