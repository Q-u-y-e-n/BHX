using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")] // Chỉ nhân viên cửa hàng mới được truy cập
    public class KhachHangController : Controller
    {
        private readonly BHXContext _context;

        public KhachHangController(BHXContext context)
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
        // 1. DANH SÁCH KHÁCH HÀNG (INDEX)
        // ============================================================
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm kiếm theo Tên hoặc Số điện thoại
                query = query.Where(k => k.TenKhachHang.Contains(searchString) || k.SoDienThoai.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await query.OrderByDescending(k => k.KhachHangID).ToListAsync());
        }

        // ============================================================
        // 2. CHI TIẾT HỒ SƠ & LỊCH SỬ MUA (DETAILS)
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            int storeId = GetCurrentStoreId();

            var khachHang = await _context.KhachHangs
                .Include(k => k.HoaDons) // Load lịch sử hóa đơn
                .FirstOrDefaultAsync(m => m.KhachHangID == id);

            if (khachHang == null) return NotFound();

            // Lọc lịch sử: Chỉ lấy hóa đơn mua TẠI CỬA HÀNG NÀY
            var lichSuTaiShop = khachHang.HoaDons
                                    .Where(h => h.CuaHangID == storeId)
                                    .OrderByDescending(h => h.NgayLap)
                                    .ToList();

            ViewBag.LichSuMuaHang = lichSuTaiShop;

            // Tính tổng tiền khách đã chi tiêu tại shop này (để chăm sóc khách VIP)
            ViewBag.TongChiTieu = lichSuTaiShop.Sum(h => h.TongTien);

            return View(khachHang);
        }

        // ============================================================
        // 3. THÊM KHÁCH HÀNG MỚI (CREATE)
        // ============================================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng SĐT trong hồ sơ khách hàng
                if (await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == model.SoDienThoai))
                {
                    ModelState.AddModelError("SoDienThoai", "Khách hàng có số điện thoại này đã tồn tại.");
                    return View(model);
                }

                // 2. Bắt đầu Giao dịch (Transaction)
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // A. Lưu Hồ Sơ Khách Hàng (Vào bảng KhachHang)
                        _context.KhachHangs.Add(model);
                        await _context.SaveChangesAsync();

                        // B. Kiểm tra và Tạo Tài khoản Đăng nhập (Vào bảng Users)
                        // Nếu SĐT này chưa có tài khoản thì tạo giúp họ luôn
                        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.SoDienThoai);

                        if (existingUser == null)
                        {
                            // Hash mật khẩu mặc định "123456" (Chuẩn Unicode để khớp với AccountController)
                            byte[] passwordHash;
                            using (var sha256 = SHA256.Create())
                            {
                                passwordHash = sha256.ComputeHash(Encoding.Unicode.GetBytes("123456"));
                            }

                            var newUser = new Users
                            {
                                Username = model.SoDienThoai, // Tên đăng nhập = SĐT
                                PasswordHash = passwordHash,
                                HoTen = model.TenKhachHang,
                                SoDienThoai = model.SoDienThoai,
                                LoaiTaiKhoan = "Customer", // Vai trò là Khách
                                TrangThai = "Hoạt động",
                                CuaHangID = null // Khách không quản lý cửa hàng nào
                            };

                            _context.Users.Add(newUser);
                            await _context.SaveChangesAsync();

                            // C. Gán quyền "Customer" trong bảng UserRoles
                            var roleCustomer = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                            if (roleCustomer != null)
                            {
                                _context.UserRoles.Add(new UserRoles
                                {
                                    UserID = newUser.UserID,
                                    RoleID = roleCustomer.RoleID
                                });
                                await _context.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = $"Đã thêm khách thành công! Tài khoản đăng nhập: {model.SoDienThoai} (MK: 123456)";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    }
                }
            }
            return View(model);
        }

        // ============================================================
        // 4. CHỈNH SỬA THÔNG TIN (EDIT)
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null) return NotFound();

            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhachHang model)
        {
            if (id != model.KhachHangID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    // (Tùy chọn) Có thể cập nhật luôn tên trong bảng Users nếu muốn đồng bộ
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.SoDienThoai);
                    if (user != null)
                    {
                        user.HoTen = model.TenKhachHang;
                        _context.Update(user);
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.KhachHangs.Any(e => e.KhachHangID == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}