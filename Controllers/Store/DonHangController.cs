using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")]
    public class DonHangController : Controller
    {
        private readonly BHXContext _context;

        public DonHangController(BHXContext context)
        {
            _context = context;
        }

        private int GetCurrentStoreId()
        {
            var claim = User.FindFirst("CuaHangID");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // 1. DANH SÁCH ĐƠN HÀNG
        public async Task<IActionResult> Index()
        {
            int storeId = GetCurrentStoreId();
            if (storeId == 0) return RedirectToAction("Login", "Account", new { area = "" });

            var list = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Where(d => d.CuaHangID == storeId)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(list);
        }

        // 2. CHI TIẾT ĐƠN HÀNG
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            int storeId = GetCurrentStoreId();

            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(m => m.DonHangID == id && m.CuaHangID == storeId);

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // ====================================================================
        // 3. CẬP NHẬT TRẠNG THÁI & XỬ LÝ KHO TỰ ĐỘNG
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string trangThai)
        {
            int storeId = GetCurrentStoreId();

            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DonHangID == id && d.CuaHangID == storeId);

            if (donHang == null) return NotFound();

            // --- TRƯỜNG HỢP 1: BẤM "BẮT ĐẦU GIAO HÀNG" ---
            if (trangThai == "Đang giao" && donHang.TrangThai == "Chờ xác nhận")
            {
                // Bước 1: Kiểm tra xem có món nào thiếu hàng không?
                var hangThieu = new List<(int SanPhamID, int SoLuongThieu, string TenSP)>();

                foreach (var item in donHang.ChiTietDonHangs)
                {
                    var tonKho = await _context.TonKho_CuaHangs
                        .FirstOrDefaultAsync(t => t.CuaHangID == storeId && t.SanPhamID == item.SanPhamID);

                    int soLuongCo = tonKho?.SoLuong ?? 0;

                    if (soLuongCo < item.SoLuong)
                    {
                        hangThieu.Add((item.SanPhamID, item.SoLuong - soLuongCo, item.SanPham?.TenSanPham ?? "SP"));
                    }
                }

                // Bước 2: Nếu có hàng thiếu -> TẠO PHIẾU ĐỀ XUẤT TỰ ĐỘNG
                if (hangThieu.Any())
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // Tạo phiếu đề xuất
                            var deXuat = new DeXuatNhapHang
                            {
                                CuaHangID = storeId,
                                NgayDeXuat = DateTime.Now,
                                TrangThai = "Chờ duyệt"
                            };
                            _context.DeXuatNhapHangs.Add(deXuat);
                            await _context.SaveChangesAsync();

                            // Tạo chi tiết đề xuất (Nhập đúng số lượng thiếu + 10 cái dự phòng)
                            foreach (var item in hangThieu)
                            {
                                _context.ChiTietDeXuatNhaps.Add(new ChiTietDeXuatNhap
                                {
                                    DeXuatID = deXuat.DeXuatID,
                                    SanPhamID = item.SanPhamID,
                                    SoLuong = item.SoLuongThieu + 10
                                });
                            }
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            // Báo lỗi ra màn hình và DỪNG LẠI (Không đổi trạng thái đơn)
                            string msgDetails = string.Join(", ", hangThieu.Select(x => x.TenSP));
                            TempData["ErrorMessage"] = "Không đủ hàng để giao! Kho đang thiếu: " + msgDetails;
                            TempData["WarningMessage"] = $"Hệ thống đã tự động tạo Phiếu Đề Xuất Nhập (Mã #{deXuat.DeXuatID}). Vui lòng chờ hàng về và nhập kho trước khi giao đơn này.";

                            return RedirectToAction(nameof(Details), new { id = id });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = "Lỗi khi tạo đề xuất tự động: " + ex.Message;
                            return RedirectToAction(nameof(Details), new { id = id });
                        }
                    }
                }

                // Bước 3: Nếu ĐỦ HÀNG -> Trừ kho và Đổi trạng thái
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var item in donHang.ChiTietDonHangs)
                        {
                            var tonKho = await _context.TonKho_CuaHangs
                                .FirstAsync(t => t.CuaHangID == storeId && t.SanPhamID == item.SanPhamID);

                            tonKho.SoLuong -= item.SoLuong; // Trừ tồn kho
                            _context.Update(tonKho);
                        }

                        donHang.TrangThai = "Đang giao";
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Kho đủ hàng. Đã trừ tồn kho và bắt đầu đi giao!";
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Lỗi trừ kho: " + ex.Message;
                    }
                }
            }
            // --- TRƯỜNG HỢP 2: HOÀN THÀNH ĐƠN ---
            else if (trangThai == "Hoàn thành" && donHang.TrangThai == "Đang giao")
            {
                donHang.TrangThai = "Hoàn thành";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đơn hàng đã hoàn tất thành công!";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}