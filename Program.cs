using BHX_Web.Data; // Thêm
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm
using Microsoft.EntityFrameworkCore; // Thêm

var builder = WebApplication.CreateBuilder(args);

// 1. Lấy chuỗi kết nối
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Thêm DbContext (Sử dụng BHXContext của bạn)
builder.Services.AddDbContext<BHXContext>(options =>
    options.UseSqlServer(connectionString));

// 3. THÊM DỊCH VỤ COOKIE AUTHENTICATION (Thay cho Identity)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.LoginPath = "/Account/Login"; // Đường dẫn đến trang Login
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn khi bị cấm
        options.SlidingExpiration = true;
    });

// 4. Thêm Controllers và Views
builder.Services.AddControllersWithViews();

// (Tùy chọn) Thêm HttpContextAccessor để lấy thông tin user ở mọi nơi
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ----- CẤU HÌNH HTTP REQUEST PIPELINE -----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 5. KÍCH HOẠT AUTHENTICATION & AUTHORIZATION
// (Thứ tự rất quan trọng)
app.UseAuthentication(); // Xác thực (Bạn là ai?)
app.UseAuthorization();  // Phân quyền (Bạn được làm gì?)

// 6. Cấu hình Areas (Như bạn đã có)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();