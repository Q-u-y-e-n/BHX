using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BHX_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly BHXContext _context;

        public AccountController(BHXContext context)
        {
            _context = context;
        }

        // ---------- ĐĂNG NHẬP ----------

        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì về trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Tìm User trong DB
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }

                // 2. Kiểm tra Mật khẩu (Sử dụng BCrypt)
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

                if (isPasswordValid)
                {
                    // 3. Lấy Role của User
                    var userRole = await _context.UserRoles
                        .Include(ur => ur.Role) // Join với bảng Roles
                        .Where(ur => ur.UserID == user.UserID)
                        .Select(ur => ur.Role.RoleName) // Chỉ lấy RoleName
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(userRole))
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản chưa được gán vai trò.");
                        return View(model);
                    }

                    // 4. Tạo "Claims" (Thông tin định danh)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim("FullName", user.FullName ?? ""),
                        new Claim(ClaimTypes.Role, userRole) // <<-- GÁN ROLE VÀO ĐÂY
                    };

                    // 5. Tạo "ClaimsIdentity" và "Principal"
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Tùy chọn: "Ghi nhớ tôi"
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    };

                    // 6. Đăng nhập (Tạo Cookie)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 7. PHÂN QUYỀN CHUYỂN HƯỚNG
                    // Đây chính là logic bạn yêu cầu
                    return RedirectToRole(userRole);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }
            }
            return View(model);
        }

        // Hàm hỗ trợ chuyển hướng
        private IActionResult RedirectToRole(string role)
        {
            switch (role)
            {
                case "Admin":
                    // Chuyển hướng đến Area "Admin", Controller "Home", Action "Index"
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                case "Store":
                    // Chuyển hướng đến Area "Store", Controller "Home", Action "Index"
                    return RedirectToAction("Index", "Home", new { area = "Store" });
                case "Customer":
                    // Chuyển hướng về trang chủ (hoặc trang Customer)
                    return RedirectToAction("Index", "Home", new { area = "Customer" });
                default:
                    // Mặc định về trang chủ
                    return RedirectToAction("Index", "Home");
            }
        }

        // ---------- ĐĂNG XUẤT ----------
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Xóa cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // ---------- TRANG CẤM TRUY CẬP ----------
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View(); // Tạo một View AccessDenied.cshtml đơn giản
        }


        // ---------- (TÙY CHỌN) ĐĂNG KÝ (Tạo tài khoản) ----------
        // Dùng để tạo tài khoản (ví dụ: tạo tài khoản Customer)

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra Username đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }

                // 2. Băm mật khẩu (Sử dụng BCrypt)
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // 3. Tạo User mới
                var newUser = new Users
                {
                    Username = model.Username,
                    PasswordHash = passwordHash,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 4. Gán Role "Customer" (Mặc định)
                // (Bạn cần đảm bảo Role "Customer" có trong bảng Roles)
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                if (customerRole != null)
                {
                    var userRole = new UserRoles
                    {
                        UserID = newUser.UserID,
                        RoleID = customerRole.RoleID
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }

                // Chuyển về trang đăng nhập
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }
    }
}