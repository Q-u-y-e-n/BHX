using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CuaHangController : Controller
    {
        private readonly BHXContext _context;

        public CuaHangController(BHXContext context)
        {
            _context = context;
        }

        // =========================================================
        // 1. DANH SÁCH CỬA HÀNG
        // =========================================================
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.CuaHangs.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Sửa lỗi CS8602: Kiểm tra null trước khi dùng Contains
                query = query.Where(c => (c.TenCuaHang != null && c.TenCuaHang.Contains(searchString))
                                      || (c.DiaChi != null && c.DiaChi.Contains(searchString)));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await query.OrderByDescending(c => c.CuaHangID).ToListAsync());
        }

        // =========================================================
        // 2. THÊM MỚI (GET) - Hiển thị form
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateStoreViewModel());
        }

        // =========================================================
        // 2. THÊM MỚI (POST) - Xử lý tạo Cửa hàng + User
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStoreViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra trùng User
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                return View(model);
            }

            // Bắt đầu Giao dịch (Transaction)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tạo Cửa Hàng
                    var newStore = new CuaHang
                    {
                        TenCuaHang = model.TenCuaHang,
                        DiaChi = model.DiaChi,
                        SoDienThoai = model.SoDienThoai,
                        TrangThai = "Hoạt động"
                    };
                    _context.CuaHangs.Add(newStore);
                    await _context.SaveChangesAsync(); // Lưu để lấy ID

                    // 2. Mã hóa mật khẩu (Unicode)
                    byte[] passwordHash;
                    using (var sha256 = SHA256.Create())
                    {
                        passwordHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.Password));
                    }

                    // 3. Tạo User Quản lý
                    var newUser = new Users
                    {
                        Username = model.Username,
                        PasswordHash = passwordHash,
                        HoTen = "Quản lý: " + (model.TenCuaHang ?? "Mới"), // Fix lỗi null
                        SoDienThoai = model.SoDienThoai,
                        LoaiTaiKhoan = "Store",
                        TrangThai = "Hoạt động",
                        CuaHangID = newStore.CuaHangID // Liên kết với Cửa hàng vừa tạo
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 4. Gán quyền Store
                    var roleStore = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Store");
                    if (roleStore != null)
                    {
                        _context.UserRoles.Add(new UserRoles
                        {
                            UserID = newUser.UserID,
                            RoleID = roleStore.RoleID
                        });
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync(); // Hoàn tất
                    TempData["SuccessMessage"] = $"Đã tạo cửa hàng và tài khoản '{model.Username}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // Hủy nếu lỗi
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    return View(model);
                }
            }
        }

        // =========================================================
        // 3. CHỈNH SỬA (GET)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var cuaHang = await _context.CuaHangs.FindAsync(id);
            if (cuaHang == null) return NotFound();
            return View(cuaHang);
        }

        // =========================================================
        // 3. CHỈNH SỬA (POST)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CuaHang model)
        {
            if (id != model.CuaHangID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CuaHangs.Any(e => e.CuaHangID == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // =========================================================
        // 4. ĐỔI TRẠNG THÁI
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var cuaHang = await _context.CuaHangs.FindAsync(id);
            if (cuaHang == null) return NotFound();

            if (cuaHang.TrangThai == "Hoạt động") cuaHang.TrangThai = "Tạm ngừng";
            else cuaHang.TrangThai = "Hoạt động";

            _context.Update(cuaHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã đổi trạng thái thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}