using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")]
    public class ThongKeController : Controller
    {
        private readonly BHXContext _context;

        public ThongKeController(BHXContext context)
        {
            _context = context;
        }

        private int GetCurrentStoreId() => int.Parse(User.FindFirst("CuaHangID")?.Value ?? "0");

        // ============================================================
        // 1. XEM THỐNG KÊ (INDEX) - GỘP OFFLINE + ONLINE
        // ============================================================
        public async Task<IActionResult> Index(int? thang, int? nam)
        {
            int storeId = GetCurrentStoreId();
            if (storeId == 0) return RedirectToAction("Login", "Account", new { area = "" });

            int t = thang ?? DateTime.Now.Month;
            int n = nam ?? DateTime.Now.Year;

            var model = new ThongKeViewModel
            {
                Thang = t,
                Nam = n
            };

            // --- PHẦN 1: LẤY DỮ LIỆU OFFLINE (BÁN TẠI QUẦY) ---
            var offlineSales = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.HoaDon.CuaHangID == storeId
                          && ct.HoaDon.NgayLap.Month == t
                          && ct.HoaDon.NgayLap.Year == n)
                .Select(ct => new
                {
                    ct.SanPhamID,
                    SoLuong = ct.SoLuong,
                    DoanhThu = ct.SoLuong * ct.DonGia
                })
                .ToListAsync();

            // --- PHẦN 2: LẤY DỮ LIỆU ONLINE (ĐƠN WEB ĐÃ HOÀN THÀNH) ---
            var onlineSales = await _context.ChiTietDonHangs
                .Include(ct => ct.DonHang)
                .Where(ct => ct.DonHang.CuaHangID == storeId
                          && ct.DonHang.TrangThai == "Hoàn thành" // Chỉ lấy đơn thành công
                          && ct.DonHang.NgayDat.Month == t
                          && ct.DonHang.NgayDat.Year == n)
                .Select(ct => new
                {
                    ct.SanPhamID,
                    SoLuong = ct.SoLuong,
                    DoanhThu = ct.SoLuong * ct.DonGia
                })
                .ToListAsync();

            // --- PHẦN 3: GỘP DỮ LIỆU (UNION ALL) ---
            var allSales = offlineSales.Concat(onlineSales).ToList();

            // --- PHẦN 4: TÍNH TOÁN TỔNG QUAN ---
            model.TongDoanhThu = allSales.Sum(x => x.DoanhThu);

            // Đếm số đơn (Offline + Online)
            int countOffline = await _context.HoaDons.CountAsync(h => h.CuaHangID == storeId && h.NgayLap.Month == t && h.NgayLap.Year == n);
            int countOnline = await _context.DonHangs.CountAsync(d => d.CuaHangID == storeId && d.TrangThai == "Hoàn thành" && d.NgayDat.Month == t && d.NgayDat.Year == n);
            model.TongDonHang = countOffline + countOnline;

            // Tính hàng trả về (Thất thoát)
            var phieuTras = _context.ChiTietTraHangs
                .Include(ct => ct.DanhSachTraHang)
                .Include(ct => ct.SanPham)
                .Where(ct => ct.DanhSachTraHang.CuaHangID == storeId
                          && ct.DanhSachTraHang.NgayTra.Month == t
                          && ct.DanhSachTraHang.NgayTra.Year == n);
            model.TongGiaTriTraHang = await phieuTras.SumAsync(ct => ct.SoLuong * ct.SanPham.GiaNhap);

            // --- PHẦN 5: CHI TIẾT TỪNG SẢN PHẨM (GROUP BY) ---
            // Lấy danh sách tên sản phẩm để hiển thị (vì query trên chỉ lấy ID cho nhẹ)
            var productInfo = await _context.SanPhams.ToDictionaryAsync(k => k.SanPhamID, v => new { v.TenSanPham, v.DonViTinh });

            model.SanPhamBanChay = allSales
                .GroupBy(x => x.SanPhamID)
                .Select(g => new ChiTietBanHang
                {
                    SanPhamID = g.Key,
                    TenSanPham = productInfo.ContainsKey(g.Key) ? productInfo[g.Key].TenSanPham : "SP đã xóa",
                    DonViTinh = productInfo.ContainsKey(g.Key) ? productInfo[g.Key].DonViTinh : "",
                    SoLuongBan = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.DoanhThu)
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            return View(model);
        }

        // ============================================================
        // 2. GỬI BÁO CÁO (CŨNG PHẢI GỘP CẢ 2 NGUỒN)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> BaoCaoDoanhThu(int thang, int nam)
        {
            int storeId = GetCurrentStoreId();

            // 1. Lấy Offline
            var offline = await _context.ChiTietHoaDons
                .Where(ct => ct.HoaDon.CuaHangID == storeId && ct.HoaDon.NgayLap.Month == thang && ct.HoaDon.NgayLap.Year == nam)
                .Select(ct => new { ct.SanPhamID, ct.SoLuong, DoanhThu = ct.SoLuong * ct.DonGia })
                .ToListAsync();

            // 2. Lấy Online
            var online = await _context.ChiTietDonHangs
                .Where(ct => ct.DonHang.CuaHangID == storeId && ct.DonHang.TrangThai == "Hoàn thành" && ct.DonHang.NgayDat.Month == thang && ct.DonHang.NgayDat.Year == nam)
                .Select(ct => new { ct.SanPhamID, ct.SoLuong, DoanhThu = ct.SoLuong * ct.DonGia })
                .ToListAsync();

            // 3. Gộp và Group
            var totalStats = offline.Concat(online)
                .GroupBy(x => x.SanPhamID)
                .Select(g => new
                {
                    SanPhamID = g.Key,
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.DoanhThu)
                })
                .ToList();

            if (!totalStats.Any())
            {
                TempData["ErrorMessage"] = "Không có dữ liệu bán hàng để báo cáo.";
                return RedirectToAction(nameof(Index), new { thang, nam });
            }

            // 4. Lưu vào DB
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Xóa báo cáo cũ
                    var oldData = _context.BanHang_TongHops.Where(b => b.CuaHangID == storeId && b.Thang == thang && b.Nam == nam);
                    _context.BanHang_TongHops.RemoveRange(oldData);
                    await _context.SaveChangesAsync();

                    // Thêm mới
                    foreach (var item in totalStats)
                    {
                        _context.BanHang_TongHops.Add(new BanHang_TongHop
                        {
                            CuaHangID = storeId,
                            SanPhamID = item.SanPhamID,
                            SoLuongBan = item.SoLuong,
                            DoanhThu = item.DoanhThu,
                            Thang = thang,
                            Nam = nam
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã gửi báo cáo tổng hợp (Offline + Online) thành công!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index), new { thang, nam });
        }
    }
}