using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;
using BHX_Web.Helpers; // Cần namespace này để dùng Session Extension

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

            // 1. Hash mật khẩu
            byte[] inputHash;
            using (var sha256 = SHA256.Create())
            {
                inputHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.Password));
            }

            // 2. Tìm User
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || user.TrangThai != "Hoạt động" || !user.PasswordHash.SequenceEqual(inputHash))
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                return View(model);
            }

            // 3. Tạo Claims
            var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Customer";

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.GivenName, user.HoTen ?? user.Username),
        new Claim(ClaimTypes.Role, roleName),
        new Claim("UserID", user.UserID.ToString()) // <--- BẮT BUỘC PHẢI CÓ CÁI NÀY
    };

            if (user.CuaHangID != null)
            {
                claims.Add(new Claim("CuaHangID", user.CuaHangID.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddMinutes(60)
            };

            // 4. Ghi Cookie (Đăng nhập thành công)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // ================================================================
            // 🔥 KHÔI PHỤC GIỎ HÀNG TỪ SQL VÀO SESSION 🔥
            // ================================================================
            try
            {
                // Lấy dữ liệu từ bảng GioHang
                var dbCart = await _context.GioHangs
                    .Include(g => g.SanPham) // Load thông tin sản phẩm (Tên, Giá, Ảnh)
                    .Where(g => g.UserID == user.UserID)
                    .ToListAsync();

                if (dbCart.Any())
                {
                    var sessionCart = new List<GioHangItem>();
                    foreach (var item in dbCart)
                    {
                        if (item.SanPham != null) // Kiểm tra null để tránh lỗi
                        {
                            sessionCart.Add(new GioHangItem
                            {
                                SanPhamID = item.SanPhamID,
                                TenSanPham = item.SanPham.TenSanPham,
                                HinhAnh = item.SanPham.HinhAnh ?? "",
                                DonGia = item.SanPham.GiaBan,
                                SoLuong = item.SoLuong
                            });
                        }
                    }

                    // Ghi đè vào Session
                    HttpContext.Session.Set("Online_Cart", sessionCart);
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần (Console.WriteLine(ex.Message))
            }
            // ================================================================

            // 5. Điều hướng
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

        // ==========================================
        // 3. ĐĂNG XUẤT
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); // Xóa sạch Session
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectByRole(string? role)
        {
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Index", "Home", new { area = "Customer" });
            string r = role.Trim().ToLower();

            if (r == "admin") return RedirectToAction("Index", "Home", new { area = "Admin" });
            if (r == "store") return RedirectToAction("Index", "Home", new { area = "Store" });

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }
}