using BHX_Web.Data; // Thêm
using BHX_Web.Models.Entities; // Thêm
using Microsoft.AspNetCore.Identity; // Thêm
using Microsoft.EntityFrameworkCore; // Thêm

var builder = WebApplication.CreateBuilder(args);

// 1. Lấy chuỗi kết nối (Bạn đã có)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); // Thay "DefaultConnection" bằng tên thật của bạn trong appsettings.json

// 2. Thêm DbContext (Bạn đã có, nhưng đảm bảo nó đúng)
builder.Services.AddDbContext<BHXContext>(options =>
    options.UseSqlServer(connectionString));

// 3. THÊM IDENTITY VÀO DỊCH VỤ (Service)
// Nó sẽ sử dụng AppUser và IdentityRole (mặc định)
// Và lưu trữ dữ liệu bằng BHXContext
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Cấu hình Identity (tùy chọn)
    options.SignIn.RequireConfirmedAccount = false; // Tạm thời tắt xác thực email
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6; // Đặt mật khẩu đơn giản
})
    .AddEntityFrameworkStores<BHXContext>()
    .AddDefaultTokenProviders(); // Cần cho việc reset mật khẩu

// 4. Cấu hình Cookie Authentication
// Identity mặc định dùng Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.LoginPath = "/Account/Login"; // Đường dẫn đến trang Login (Chúng ta sẽ tạo sau)
    options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn khi bị cấm
    options.SlidingExpiration = true;
});


// 5. Thêm Controllers và Views (Bạn đã có)
builder.Services.AddControllersWithViews();

// ... (Các dịch vụ khác nếu có) ...

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

// 6. KÍCH HOẠT AUTHENTICATION VÀ AUTHORIZATION
// QUAN TRỌNG: Phải đặt UseAuthentication() TRƯỚC UseAuthorization()
app.UseAuthentication(); // Xác thực (Xác định bạn là ai)
app.UseAuthorization();  // Phân quyền (Xác định bạn được làm gì)


// 7. Cấu hình Areas (Bạn đã có, nhưng hãy đảm bảo nó đúng)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 8. (Tùy chọn) Khởi tạo dữ liệu (Tạo Roles và Admin)
// Đây là cách để tạo tài khoản Admin đầu tiên
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        await SeedData.Initialize(userManager, roleManager); // Chúng ta sẽ tạo file SeedData
    }
    catch (Exception ex)
    {
        // Xử lý lỗi
    }
}


app.Run();