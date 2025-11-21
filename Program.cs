using BHX_Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// 1. KẾT NỐI DATABASE
// ===================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<BHXContext>(options =>
    options.UseSqlServer(connectionString));

// ===================================================
// [MỚI] 1.1. CẤU HÌNH SESSION (SỬA LỖI LOGOUT & GIỎ HÀNG)
// ===================================================
// Bắt buộc phải có đoạn này để dùng HttpContext.Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Session tồn tại 60 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===================================================
// 2. CẤU HÌNH XÁC THỰC (AUTHENTICATION)
// ===================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Cookie nhớ đăng nhập 30 ngày
        options.LoginPath = "/Account/Login";           // Đường dẫn trang đăng nhập
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Khi không đủ quyền
    });

// ===================================================
// 3. CẤU HÌNH VIEW ENGINE
// ===================================================
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Cấu hình tìm View trong thư mục Views/AreaName/... thay vì Areas/AreaName/...
        options.AreaViewLocationFormats.Add("/Views/{2}/{1}/{0}.cshtml");
        options.AreaViewLocationFormats.Add("/Views/{2}/Shared/{0}.cshtml");
        options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });

var app = builder.Build();

// ===================================================
// 4. MIDDLEWARE PIPELINE
// ===================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ===================================================
// [MỚI] 4.1. KÍCH HOẠT SESSION (QUAN TRỌNG)
// ===================================================
// Phải đặt TRƯỚC UseAuthentication
app.UseSession();

app.UseAuthentication(); // Xác thực: Bạn là ai?
app.UseAuthorization();  // Phân quyền: Bạn được làm gì?

// ===================================================
// 5. LOGIC CHUYỂN HƯỚNG THÔNG MINH
// ===================================================
// Nếu người dùng ĐÃ ĐĂNG NHẬP mà cố tình vào trang chủ "/"
// -> Hệ thống sẽ tự động đẩy về trang Dashboard tương ứng
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && context.User.Identity?.IsAuthenticated == true)
    {
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        string redirectUrl = "/Customer/Home/Index"; // Mặc định là khách

        if (role == "Tổng Công Ty") redirectUrl = "/Admin/Home/Index";
        else if (role == "Cửa Hàng") redirectUrl = "/Store/Home/Index";

        context.Response.Redirect(redirectUrl);
        return;
    }
    await next();
});

// ===================================================
// 6. ĐỊNH NGHĨA ROUTE
// ===================================================

// Route cho các Area (Admin, Store, Customer)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route mặc định (Trang chủ Public khi chưa đăng nhập)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();