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
        const string CART_KEY = "Online_Cart";

        public GioHangController(BHXContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. HELPER METHODS (Hỗ trợ xử lý)
        // ============================================================

        private List<GioHangItem> GetCartItems()
        {
            return HttpContext.Session.Get<List<GioHangItem>>(CART_KEY) ?? new List<GioHangItem>();
        }

        private void SaveCartSession(List<GioHangItem> list)
        {
            HttpContext.Session.Set(CART_KEY, list);
        }

        // Đồng bộ giỏ hàng vào SQL (Chỉ chạy khi đã đăng nhập)
        private async Task SyncSqlCart(int sanPhamId, int newQuantity)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserID");
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value);
                    var dbItem = await _context.GioHangs.FirstOrDefaultAsync(g => g.UserID == userId && g.SanPhamID == sanPhamId);

                    if (newQuantity <= 0)
                    {
                        if (dbItem != null) _context.GioHangs.Remove(dbItem);
                    }
                    else
                    {
                        if (dbItem != null)
                        {
                            dbItem.SoLuong = newQuantity;
                            _context.Update(dbItem);
                        }
                        else
                        {
                            _context.GioHangs.Add(new GioHang { UserID = userId, SanPhamID = sanPhamId, SoLuong = newQuantity });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        // ============================================================
        // 2. CÁC CHỨC NĂNG GIỎ HÀNG (Thêm/Sửa/Xóa)
        // ============================================================

        public IActionResult Index()
        {
            var cart = GetCartItems();
            ViewBag.TongTien = cart.Sum(x => x.ThanhTien);
            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.SanPhams.FindAsync(id);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.SanPhamID == id);
            int quantity = 1;

            if (item != null)
            {
                item.SoLuong++;
                quantity = item.SoLuong;
            }
            else
            {
                cart.Add(new GioHangItem
                {
                    SanPhamID = product.SanPhamID,
                    TenSanPham = product.TenSanPham,
                    DonGia = product.GiaBan,
                    HinhAnh = product.HinhAnh,
                    SoLuong = 1
                });
            }

            SaveCartSession(cart);
            await SyncSqlCart(id, quantity);
            return RedirectToAction(nameof(Index));
        }

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
                    await SyncSqlCart(id, quantity);
                }
                else
                {
                    cart.Remove(item);
                    await SyncSqlCart(id, 0);
                }
                SaveCartSession(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.SanPhamID == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartSession(cart);
                await SyncSqlCart(id, 0);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = User.FindFirst("UserID")?.Value;
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    int userId = int.Parse(userIdStr);
                    var items = _context.GioHangs.Where(g => g.UserID == userId);
                    _context.GioHangs.RemoveRange(items);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 3. THANH TOÁN (CHECKOUT) & TỰ ĐỘNG GÁN CỬA HÀNG
        // ============================================================

        [Authorize(Roles = "Customer,Admin,Store")]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any()) return RedirectToAction(nameof(Index));

            var userPhone = User.Identity?.Name;
            var userName = User.FindFirst(ClaimTypes.GivenName)?.Value;

            // Tìm thông tin khách cũ (nếu có) để điền sẵn
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == userPhone);

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                TongTien = cart.Sum(x => x.ThanhTien),
                TenNguoiNhan = userName ?? "",
                SoDienThoai = userPhone ?? "",
                DiaChi = khachHang?.DiaChi ?? ""
            };

            return View(model);
        }

        // ... (Các using và code cũ giữ nguyên)

        [Authorize(Roles = "Customer,Admin,Store")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any()) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                var userPhone = User.Identity?.Name;

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Cập nhật thông tin Khách hàng
                        var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == userPhone);
                        if (khachHang == null)
                        {
                            khachHang = new KhachHang { TenKhachHang = model.TenNguoiNhan, SoDienThoai = model.SoDienThoai, DiaChi = model.DiaChi };
                            _context.KhachHangs.Add(khachHang);
                        }
                        else
                        {
                            khachHang.TenKhachHang = model.TenNguoiNhan;
                            khachHang.DiaChi = model.DiaChi; // Cập nhật địa chỉ mới nhất
                            _context.Update(khachHang);
                        }
                        await _context.SaveChangesAsync();

                        // ==================================================================================
                        // 🔥 THUẬT TOÁN PHÂN CHIA ĐƠN HÀNG THEO KHU VỰC (ROUTING) 🔥
                        // ==================================================================================

                        int? targetStoreId = null;
                        string diaChiKhach = model.DiaChi.ToLower(); // Chuyển về chữ thường để so sánh: "quận 1"

                        // Lấy tất cả cửa hàng đang hoạt động
                        var activeStores = await _context.CuaHangs.Where(c => c.TrangThai == "Hoạt động").ToListAsync();

                        // Danh sách từ khóa Quận/Huyện (Bạn có thể mở rộng thêm)
                        // Logic: Nếu địa chỉ khách chứa từ khóa -> Gán cho cửa hàng có địa chỉ chứa từ khóa đó
                        var districtKeywords = new List<string> {
                    "quận 1", "quận 2", "quận 3", "quận 4", "quận 5", "quận 6", "quận 7", "quận 8", "quận 9", "quận 10", "quận 11", "quận 12",
                    "thủ đức", "bình thạnh", "gò vấp", "phú nhuận", "tân bình", "tân phú", "bình tân",
                    "hóc môn", "củ chi", "nhà bè", "bình chánh", "cần giờ"
                };

                        foreach (var kw in districtKeywords)
                        {
                            // Nếu địa chỉ khách có chứa từ khóa này (Ví dụ: "quận 1")
                            if (diaChiKhach.Contains(kw))
                            {
                                // Tìm cửa hàng nào cũng nằm trong khu vực đó (Địa chỉ cửa hàng chứa "quận 1")
                                var matchStore = activeStores.FirstOrDefault(s => s.DiaChi.ToLower().Contains(kw));

                                if (matchStore != null)
                                {
                                    targetStoreId = matchStore.CuaHangID;
                                    break; // Tìm thấy cửa hàng phù hợp nhất thì dừng lại ngay
                                }
                            }
                        }

                        // Nếu không tìm thấy cửa hàng nào khớp quận (hoặc khách ở tỉnh),
                        // Gán về Cửa hàng mặc định (ID=1 hoặc cửa hàng đầu tiên tìm thấy) để Admin xử lý sau
                        if (targetStoreId == null && activeStores.Any())
                        {
                            targetStoreId = activeStores.First().CuaHangID;
                        }
                        // ==================================================================================

                        // 2. Tạo Đơn Hàng
                        var donHang = new DonHang
                        {
                            KhachHangID = khachHang.KhachHangID,
                            NgayDat = DateTime.Now,
                            TrangThai = "Chờ xác nhận",
                            CuaHangID = targetStoreId // <--- QUAN TRỌNG: Đơn hàng đã được gán cho cửa hàng cụ thể
                        };
                        _context.DonHangs.Add(donHang);
                        await _context.SaveChangesAsync();

                        // 3. Lưu Chi Tiết Đơn
                        foreach (var item in cart)
                        {
                            _context.ChiTietDonHangs.Add(new ChiTietDonHang
                            {
                                DonHangID = donHang.DonHangID,
                                SanPhamID = item.SanPhamID,
                                SoLuong = item.SoLuong,
                                DonGia = item.DonGia
                            });
                        }
                        await _context.SaveChangesAsync();

                        // 4. Dọn dẹp
                        HttpContext.Session.Remove(CART_KEY);
                        if (User.Identity.IsAuthenticated)
                        {
                            var userIdStr = User.FindFirst("UserID")?.Value;
                            if (userIdStr != null)
                            {
                                int uid = int.Parse(userIdStr);
                                var dbCart = _context.GioHangs.Where(g => g.UserID == uid);
                                _context.GioHangs.RemoveRange(dbCart);
                                await _context.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        return RedirectToAction(nameof(OrderSuccess), new { id = donHang.DonHangID });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi xử lý: " + ex.Message);
                    }
                }
            }

            // Reload View nếu lỗi
            model.CartItems = cart;
            model.TongTien = cart.Sum(x => x.ThanhTien);
            return View("Checkout", model);
        }

        public IActionResult OrderSuccess(int id)
        {
            return View(id);
        }
    }
}