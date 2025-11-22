using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Cần cho [Authorize]
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;
using BHX_Web.Helpers; // Cần để dùng Session Extension

namespace BHX_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly BHXContext _context;

        public AccountController(BHXContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectByRole(User.FindFirstValue(ClaimTypes.Role));
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Hash mật khẩu (Unicode để khớp SQL HASHBYTES)
            byte[] inputHash;
            using (var sha256 = SHA256.Create())
            {
                inputHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.Password));
            }

            // 2. Tìm User
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            // 3. Kiểm tra thông tin
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }
            if (user.TrangThai != "Hoạt động")
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa.");
                return View(model);
            }
            if (!user.PasswordHash.SequenceEqual(inputHash))
            {
                ModelState.AddModelError("", "Mật khẩu không đúng.");
                return View(model);
            }

            // 4. Tạo Claims (Thông tin định danh)
            var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Customer";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.HoTen ?? user.Username),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("UserID", user.UserID.ToString())
            };

            // Nếu là quản lý cửa hàng -> Lưu thêm ID Cửa hàng
            if (user.CuaHangID != null)
            {
                claims.Add(new Claim("CuaHangID", user.CuaHangID.ToString()));
            }

            // 5. Ghi Cookie Đăng Nhập
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddMinutes(60)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // ================================================================
            // 🔥 TÍNH NĂNG: ĐỒNG BỘ GIỎ HÀNG TỪ SQL LÊN SESSION KHI LOGIN 🔥
            // ================================================================
            try
            {
                var dbCart = await _context.GioHangs
                    .Include(g => g.SanPham)
                    .Where(g => g.UserID == user.UserID)
                    .ToListAsync();

                if (dbCart.Any())
                {
                    var sessionCart = dbCart.Select(item => new GioHangItem
                    {
                        SanPhamID = item.SanPhamID,
                        TenSanPham = item.SanPham?.TenSanPham ?? "Sản phẩm",
                        HinhAnh = item.SanPham?.HinhAnh ?? "",
                        DonGia = item.SanPham?.GiaBan ?? 0,
                        SoLuong = item.SoLuong
                    }).ToList();

                    // Ghi đè vào Session hiện tại
                    HttpContext.Session.Set("Online_Cart", sessionCart);
                }
            }
            catch (Exception) { /* Bỏ qua lỗi nếu sync thất bại */ }
            // ================================================================

            // 6. Điều hướng
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectByRole(roleName);
        }

        // ==========================================
        // 2. ĐĂNG KÝ (REGISTER)
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectByRole(User.FindFirstValue(ClaimTypes.Role));
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            byte[] passwordHash;
            using (var sha256 = SHA256.Create())
            {
                passwordHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.Password));
            }

            var newUser = new Users
            {
                Username = model.Username,
                PasswordHash = passwordHash,
                HoTen = model.FullName,
                SoDienThoai = model.PhoneNumber,
                LoaiTaiKhoan = "Customer",
                TrangThai = "Hoạt động"
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Gán quyền Customer
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                if (role != null)
                {
                    _context.UserRoles.Add(new UserRoles { UserID = newUser.UserID, RoleID = role.RoleID });
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                return View(model);
            }
        }

        // ==========================================
        // 3. ĐỔI MẬT KHẨU (CHANGE PASSWORD)
        // ==========================================
        [Authorize] // Phải đăng nhập mới được vào
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdStr = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return RedirectToAction("Login");

            // Kiểm tra mật khẩu cũ
            byte[] oldHash;
            using (var sha256 = SHA256.Create())
            {
                oldHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.OldPassword));
            }

            if (!user.PasswordHash.SequenceEqual(oldHash))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác.");
                return View(model);
            }

            // Cập nhật mật khẩu mới
            using (var sha256 = SHA256.Create())
            {
                user.PasswordHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.NewPassword));
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";

            // Điều hướng về đúng trang chủ theo quyền
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectByRole(role);
        }

        // ==========================================
        // 4. ĐĂNG XUẤT (LOGOUT)
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); // Xóa sạch Session Giỏ hàng
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // ==========================================
        // 5. CÁC HÀM PHỤ TRỢ
        // ==========================================
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectByRole(string? role)
        {
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Index", "Home", new { area = "Customer" });

            // Chuẩn hóa
            string r = role.Trim();

            // So sánh chính xác (Case sensitive hoặc không tùy bạn, ở đây tôi dùng equals cho chắc)
            if (string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            if (string.Equals(r, "Store", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home", new { area = "Store" });
            }

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }
}