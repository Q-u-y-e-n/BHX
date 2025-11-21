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
            // Nếu đã đăng nhập -> chuyển hướng theo quyền luôn
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

            // [QUAN TRỌNG] Hash mật khẩu bằng UNICODE để khớp với SQL Server (HASHBYTES)
            byte[] inputHash;
            using (var sha256 = SHA256.Create())
            {
                inputHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.Password));
            }

            // Tìm User & Role (Include bảng UserRoles và Roles để lấy quyền)
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            // Kiểm tra tài khoản tồn tại
            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập không tồn tại.");
                return View(model);
            }

            // Kiểm tra trạng thái
            if (user.TrangThai != "Hoạt động")
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa hoặc tạm ngưng.");
                return View(model);
            }

            // So sánh mật khẩu (So sánh từng byte trong mảng hash)
            if (!user.PasswordHash.SequenceEqual(inputHash))
            {
                ModelState.AddModelError("", "Mật khẩu không chính xác.");
                return View(model);
            }

            // --- ĐĂNG NHẬP THÀNH CÔNG ---

            // 1. Lấy tên Role từ DB (Nếu không có role nào thì gán mặc định là Customer)
            var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Customer";

            // 2. Tạo danh sách Claims (Thông tin định danh)
            var claims = new List<Claim>
{
    // Thêm ?? "" để đảm bảo không bao giờ null
    new Claim(ClaimTypes.Name, user.Username ?? ""), 
    
    // Nếu HoTen null thì lấy Username, nếu Username cũng null thì lấy chuỗi "User"
    new Claim(ClaimTypes.GivenName, user.HoTen ?? user.Username ?? "User"), 
    
    // RoleName chắc chắn có giá trị do logic phía trên, nhưng thêm ?? cho chắc
    new Claim(ClaimTypes.Role, roleName ?? "Customer"),
    
    // UserID là int nên ToString() an toàn, nhưng cẩn thận thì cứ để nguyên
    new Claim("UserID", user.UserID.ToString())
};

            // [MỚI] 3. Nếu là tài khoản Cửa Hàng -> Lưu CuaHangID vào Cookie luôn
            // Giúp hệ thống biết user này quản lý cửa hàng nào ngay lập tức
            if (user.CuaHangID != null)
            {
                // ToString() của int? có thể trả về null, thêm ?? "" cho chắc chắn
                claims.Add(new Claim("CuaHangID", user.CuaHangID.ToString() ?? ""));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe, // Ghi nhớ đăng nhập
                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddMinutes(60)
            };

            // 4. Ghi Cookie vào trình duyệt
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // 5. Điều hướng về trang cũ nếu có (ví dụ link copy)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // 6. Điều hướng theo quyền
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

            // Kiểm tra trùng tên đăng nhập
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            // Hash mật khẩu bằng UNICODE
            byte[] passwordHash;
            using (var sha256 = SHA256.Create())
            {
                passwordHash = sha256.ComputeHash(Encoding.Unicode.GetBytes(model.Password));
            }

            // Tạo đối tượng User mới
            var newUser = new Users
            {
                Username = model.Username,
                PasswordHash = passwordHash,
                HoTen = model.FullName,
                SoDienThoai = model.PhoneNumber,
                LoaiTaiKhoan = "Customer", // Mặc định là Khách hàng
                TrangThai = "Hoạt động",
                CuaHangID = null // Khách hàng không quản lý cửa hàng
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync(); // Lưu để sinh UserID

                // Tìm Role "Customer" trong DB để gán
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

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View(model);
            }
        }

        // ==========================================
        // 3. LOGOUT (ĐĂNG XUẤT)
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            // Xóa Cookie xác thực
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Xóa Session (Giỏ hàng, dữ liệu tạm...)
            HttpContext.Session.Clear();

            // Chuyển về trang chủ chung (Public) - Area rỗng
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // Trang báo lỗi khi không đủ quyền truy cập (403)
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // 4. HÀM ĐIỀU HƯỚNG THÔNG MINH
        // ==========================================
        private IActionResult RedirectByRole(string? role)
        {
            // Nếu không có role -> Về trang khách
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Index", "Home", new { area = "Customer" });

            // Chuẩn hóa chuỗi về chữ thường, cắt khoảng trắng
            string r = role.Trim().ToLower();

            // So sánh với các Role chuẩn tiếng Anh (Admin, Store, Customer)
            if (r == "admin")
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            if (r == "store")
            {
                return RedirectToAction("Index", "Home", new { area = "Store" });
            }

            // Mặc định về Customer
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }
}