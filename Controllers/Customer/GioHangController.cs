using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;
using BHX_Web.Helpers;
using System.Security.Claims;

namespace BHX_Web.Controllers.Customer
{
    [Area("Customer")]
    public class GioHangController : Controller
    {
        private readonly BHXContext _context;
        const string CART_KEY = "Online_Cart"; // Key lưu trong Session

        public GioHangController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // HELPER: CÁC HÀM HỖ TRỢ (PRIVATE)
        // ============================================================

        // 1. Lấy giỏ hàng từ Session
        private List<GioHangItem> GetCartItems()
        {
            return HttpContext.Session.Get<List<GioHangItem>>(CART_KEY) ?? new List<GioHangItem>();
        }

        // 2. Lưu giỏ hàng vào Session
        private void SaveCartSession(List<GioHangItem> list)
        {
            HttpContext.Session.Set(CART_KEY, list);
        }

        // 3. [QUAN TRỌNG] Đồng bộ dữ liệu vào SQL Server (Chỉ chạy khi đã Login)
        private async Task SyncSqlCart(int sanPhamId, int newQuantity)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value);

                    // Tìm sản phẩm trong bảng GioHang của User này
                    var dbItem = await _context.GioHangs
                        .FirstOrDefaultAsync(g => g.UserID == userId && g.SanPhamID == sanPhamId);

                    if (newQuantity <= 0)
                    {
                        // Nếu số lượng <= 0 thì Xóa khỏi DB
                        if (dbItem != null) _context.GioHangs.Remove(dbItem);
                    }
                    else
                    {
                        if (dbItem != null)
                        {
                            // Nếu đã có -> Cập nhật số lượng
                            dbItem.SoLuong = newQuantity;
                            _context.Update(dbItem);
                        }
                        else
                        {
                            // Nếu chưa có -> Thêm mới
                            _context.GioHangs.Add(new GioHang
                            {
                                UserID = userId,
                                SanPhamID = sanPhamId,
                                SoLuong = newQuantity
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        // ============================================================
        // CÁC ACTION CHÍNH (PUBLIC)
        // ============================================================

        // 1. XEM GIỎ HÀNG
        public IActionResult Index()
        {
            var cart = GetCartItems();
            ViewBag.TongTien = cart.Sum(x => x.ThanhTien);
            return View(cart);
        }

        // 2. THÊM VÀO GIỎ (ADD TO CART)
        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.SanPhams.FindAsync(id);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.SanPhamID == id);
            int quantity = 1;

            if (item != null)
            {
                // Nếu đã có trong giỏ -> Tăng số lượng
                item.SoLuong++;
                quantity = item.SoLuong;
            }
            else
            {
                // Nếu chưa có -> Thêm mới vào List
                cart.Add(new GioHangItem
                {
                    SanPhamID = product.SanPhamID,
                    TenSanPham = product.TenSanPham,
                    DonGia = product.GiaBan,
                    HinhAnh = product.HinhAnh,
                    SoLuong = 1
                });
            }

            // Lưu Session
            SaveCartSession(cart);

            // Đồng bộ SQL (Async)
            await SyncSqlCart(id, quantity);

            return RedirectToAction(nameof(Index));
        }

        // 3. CẬP NHẬT SỐ LƯỢNG (UPDATE)
        [HttpPost]
        public async Task<IActionResult> UpdateCart(int id, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.SanPhamID == id);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.SoLuong = quantity;
                    await SyncSqlCart(id, quantity); // Lưu DB số lượng mới
                }
                else
                {
                    // Nếu chỉnh về 0 hoặc âm -> Xóa luôn
                    cart.Remove(item);
                    await SyncSqlCart(id, 0); // Lưu DB (xóa)
                }
                SaveCartSession(cart); // Lưu Session
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. XÓA SẢN PHẨM (REMOVE)
        public async Task<IActionResult> Remove(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.SanPhamID == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveCartSession(cart);

                // Đồng bộ DB (Truyền 0 để hàm Sync tự hiểu là xóa)
                await SyncSqlCart(id, 0);
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. XÓA HẾT GIỎ (CLEAR ALL)
        public async Task<IActionResult> Clear()
        {
            // Xóa sạch Session
            HttpContext.Session.Remove(CART_KEY);

            // Xóa sạch trong DB nếu đang đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value);
                    // Lấy tất cả dòng của user này và xóa
                    var items = _context.GioHangs.Where(g => g.UserID == userId);
                    _context.GioHangs.RemoveRange(items);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. THANH TOÁN (CHECKOUT)
        [Authorize(Roles = "Customer,Admin,Store")] // Yêu cầu đăng nhập
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            // Lấy thông tin User hiện tại
            var userIdStr = User.FindFirst("UserID")?.Value;
            var userPhone = User.Identity?.Name; // Username là Số điện thoại
            var userName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "Khách hàng";

            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            // --- BẮT ĐẦU TRANSACTION ĐỂ TẠO ĐƠN HÀNG ---
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // A. Kiểm tra xem User này đã có trong bảng KhachHang chưa?
                    // (Bảng Users dùng để Login, bảng KhachHang dùng để lưu lịch sử mua/tích điểm)
                    var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == userPhone);

                    if (khachHang == null)
                    {
                        // Nếu chưa có -> Tạo mới Khách Hàng tự động
                        khachHang = new KhachHang
                        {
                            TenKhachHang = userName,
                            SoDienThoai = userPhone,
                            DiaChi = "Cập nhật sau"
                        };
                        _context.KhachHangs.Add(khachHang);
                        await _context.SaveChangesAsync();
                    }

                    // B. Tạo Đơn Hàng Mới
                    var donHang = new DonHang
                    {
                        KhachHangID = khachHang.KhachHangID,
                        NgayDat = DateTime.Now,
                        TrangThai = "Chờ xác nhận"
                    };
                    _context.DonHangs.Add(donHang);
                    await _context.SaveChangesAsync(); // Lưu để lấy DonHangID

                    // C. Lưu Chi Tiết Đơn Hàng
                    foreach (var item in cart)
                    {
                        var chiTiet = new ChiTietDonHang
                        {
                            DonHangID = donHang.DonHangID,
                            SanPhamID = item.SanPhamID,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia
                        };
                        _context.ChiTietDonHangs.Add(chiTiet);
                    }
                    await _context.SaveChangesAsync();

                    // D. Xóa Giỏ hàng (Session + DB) sau khi đặt xong
                    HttpContext.Session.Remove(CART_KEY);

                    int userId = int.Parse(userIdStr);
                    var cartItemsDb = _context.GioHangs.Where(g => g.UserID == userId);
                    _context.GioHangs.RemoveRange(cartItemsDb);
                    await _context.SaveChangesAsync();

                    // E. Hoàn tất
                    await transaction.CommitAsync();

                    // Chuyển đến trang thông báo thành công (Cần tạo View Success)
                    // Hoặc tạm thời hiện thông báo và về trang chủ
                    TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là #{donHang.DonHangID}";
                    return RedirectToAction("Index", "DonHang", new { area = "Customer" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi khi đặt hàng: " + ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }
        }
    }
}