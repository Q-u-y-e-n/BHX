using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;
using BHX_Web.Helpers; // Using Helper vừa tạo

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")]
    public class BanHangController : Controller
    {
        private readonly BHXContext _context;

        public BanHangController(BHXContext context)
        {
            _context = context;
        }

        // Lấy ID Cửa hàng hiện tại
        private int GetStoreId() => int.Parse(User.FindFirst("CuaHangID")?.Value ?? "0");

        // 1. GIAO DIỆN BÁN HÀNG
        public async Task<IActionResult> Index(string searchString)
        {
            int storeId = GetStoreId();

            // Lấy danh sản phẩm CÓ TRONG KHO cửa hàng
            var products = _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId && t.SoLuong > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.SanPham.TenSanPham.Contains(searchString));
            }

            ViewBag.Products = await products.Take(10).ToListAsync(); // Chỉ hiện 10 sp gợi ý

            // Lấy giỏ hàng hiện tại từ Session
            var cart = HttpContext.Session.Get<List<GioHangItem>>("POS_Cart") ?? new List<GioHangItem>();
            return View(cart);
        }

        // 2. THÊM VÀO HÓA ĐƠN
        public async Task<IActionResult> AddToBill(int id)
        {
            int storeId = GetStoreId();
            var stock = await _context.TonKho_CuaHangs.Include(t => t.SanPham)
                                .FirstOrDefaultAsync(t => t.CuaHangID == storeId && t.SanPhamID == id);

            if (stock != null)
            {
                var cart = HttpContext.Session.Get<List<GioHangItem>>("POS_Cart") ?? new List<GioHangItem>();
                var item = cart.FirstOrDefault(p => p.SanPhamID == id);

                if (item != null)
                {
                    // Kiểm tra tồn kho trước khi cộng
                    if (item.SoLuong + 1 <= stock.SoLuong) item.SoLuong++;
                }
                else
                {
                    cart.Add(new GioHangItem
                    {
                        SanPhamID = stock.SanPhamID,
                        TenSanPham = stock.SanPham.TenSanPham,
                        DonGia = stock.SanPham.GiaBan,
                        HinhAnh = stock.SanPham.HinhAnh,
                        SoLuong = 1
                    });
                }
                HttpContext.Session.Set("POS_Cart", cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // 3. XÓA KHỎI HÓA ĐƠN
        public IActionResult RemoveFromBill(int id)
        {
            var cart = HttpContext.Session.Get<List<GioHangItem>>("POS_Cart") ?? new List<GioHangItem>();
            var item = cart.FirstOrDefault(p => p.SanPhamID == id);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set("POS_Cart", cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. THANH TOÁN (Checkout)
        [HttpPost]
        public async Task<IActionResult> Checkout(string TenKhachHang, string SoDienThoai)
        {
            var cart = HttpContext.Session.Get<List<GioHangItem>>("POS_Cart");
            if (cart == null || !cart.Any()) return RedirectToAction(nameof(Index));

            int storeId = GetStoreId();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // A. Xử lý khách hàng (Nếu chưa có thì tạo mới nhanh)
                    int? khachHangId = null;
                    if (!string.IsNullOrEmpty(SoDienThoai))
                    {
                        var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == SoDienThoai);
                        if (kh == null)
                        {
                            kh = new KhachHang { TenKhachHang = TenKhachHang ?? "Khách vãng lai", SoDienThoai = SoDienThoai };
                            _context.KhachHangs.Add(kh);
                            await _context.SaveChangesAsync();
                        }
                        khachHangId = kh.KhachHangID;
                    }

                    // B. Tạo Hóa Đơn
                    var hoaDon = new HoaDon
                    {
                        CuaHangID = storeId,
                        KhachHangID = khachHangId ?? 1, // Giả sử ID 1 là khách vãng lai mặc định
                        NgayLap = DateTime.Now,
                        TongTien = cart.Sum(x => x.ThanhTien)
                    };
                    _context.HoaDons.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    // C. Lưu Chi Tiết & TRỪ KHO
                    foreach (var item in cart)
                    {
                        // Trừ kho
                        var kho = await _context.TonKho_CuaHangs
                            .FirstOrDefaultAsync(k => k.CuaHangID == storeId && k.SanPhamID == item.SanPhamID);

                        if (kho == null || kho.SoLuong < item.SoLuong)
                            throw new Exception($"Sản phẩm {item.TenSanPham} không đủ hàng để bán.");

                        kho.SoLuong -= item.SoLuong;
                        _context.Update(kho);

                        // Lưu chi tiết HD
                        _context.ChiTietHoaDons.Add(new ChiTietHoaDon
                        {
                            HoaDonID = hoaDon.HoaDonID,
                            SanPhamID = item.SanPhamID,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Xóa giỏ hàng sau khi bán xong
                    HttpContext.Session.Remove("POS_Cart");
                    TempData["SuccessMessage"] = "Thanh toán thành công! Đã in hóa đơn.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}