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
    public class DeXuatNhapController : Controller
    {
        private readonly BHXContext _context;

        public DeXuatNhapController(BHXContext context)
        {
            _context = context;
        }

        private int GetCurrentStoreId() => int.Parse(User.FindFirst("CuaHangID")?.Value ?? "0");

        // 1. LỊCH SỬ ĐỀ XUẤT
        public async Task<IActionResult> Index()
        {
            int storeId = GetCurrentStoreId();
            var list = await _context.DeXuatNhapHangs
                .Where(d => d.CuaHangID == storeId)
                .OrderByDescending(d => d.NgayDeXuat)
                .ToListAsync();
            return View(list);
        }

        // 2. QUÉT NHU CẦU TỰ ĐỘNG (GET)
        [HttpGet]
        public async Task<IActionResult> AutoSuggest()
        {
            int storeId = GetCurrentStoreId();
            var model = new DeXuatViewModel();

            // A. Lấy tất cả sản phẩm đang kinh doanh tại cửa hàng
            var allStock = await _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId)
                .ToListAsync();

            // B. Tính tổng nhu cầu của các đơn hàng "Chờ xác nhận" (Những đơn chưa được trừ kho)
            var pendingDemand = await _context.ChiTietDonHangs
                .Include(ct => ct.DonHang)
                .Where(ct => ct.DonHang.CuaHangID == storeId && ct.DonHang.TrangThai == "Chờ xác nhận")
                .GroupBy(ct => ct.SanPhamID)
                .Select(g => new { SanPhamID = g.Key, TongCan = g.Sum(x => x.SoLuong) })
                .ToListAsync();

            // C. So sánh và Lọc ra danh sách cần nhập
            foreach (var item in allStock)
            {
                if (item.SanPham == null) continue;

                bool needsRestock = false;
                int quantitySuggest = 0;

                // Tìm nhu cầu đang chờ của sản phẩm này
                var demandItem = pendingDemand.FirstOrDefault(p => p.SanPhamID == item.SanPhamID);
                int totalNeed = demandItem?.TongCan ?? 0;

                // -- Điều kiện 1: Thiếu hàng trả đơn --
                if (item.SoLuong < totalNeed)
                {
                    needsRestock = true;
                    int thieu = totalNeed - item.SoLuong;
                    quantitySuggest = thieu + 20; // Nhập bù số thiếu + 20 cái bán lai rai
                }
                // -- Điều kiện 2: Tồn kho dưới định mức an toàn (10) --
                else if (item.SoLuong < 10)
                {
                    needsRestock = true;
                    quantitySuggest = 50; // Nhập lô tiêu chuẩn
                }

                if (needsRestock)
                {
                    model.Items.Add(new DeXuatItem
                    {
                        SanPhamID = item.SanPhamID,
                        TenSanPham = item.SanPham.TenSanPham,
                        DonViTinh = item.SanPham.DonViTinh,
                        HinhAnh = item.SanPham.HinhAnh,
                        TonKhoHienTai = item.SoLuong,
                        SoLuongDeXuat = quantitySuggest
                    });
                }
            }

            return View(model);
        }

        // 3. GỬI YÊU CẦU (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeXuatViewModel model)
        {
            int storeId = GetCurrentStoreId();

            // Chỉ lấy những dòng có số lượng > 0
            var validItems = model.Items?.Where(x => x.SoLuongDeXuat > 0).ToList();

            if (validItems != null && validItems.Any())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var deXuat = new DeXuatNhapHang
                        {
                            CuaHangID = storeId,
                            NgayDeXuat = DateTime.Now,
                            TrangThai = "Chờ duyệt"
                        };
                        _context.DeXuatNhapHangs.Add(deXuat);
                        await _context.SaveChangesAsync();

                        foreach (var item in validItems)
                        {
                            _context.ChiTietDeXuatNhaps.Add(new ChiTietDeXuatNhap
                            {
                                DeXuatID = deXuat.DeXuatID,
                                SanPhamID = item.SanPhamID,
                                SoLuong = item.SoLuongDeXuat
                            });
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Đã gửi yêu cầu nhập hàng thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Chưa chọn sản phẩm nào để nhập.");
            }

            return View("AutoSuggest", model);
        }

        // 4. TẠO THỦ CÔNG (GET)
        [HttpGet]
        public IActionResult CreateManual()
        {
            int storeId = GetCurrentStoreId();

            var listSp = _context.SanPhams
                .Select(s => new
                {
                    s.SanPhamID,
                    TenHienThi = s.TenSanPham,
                    s.DonViTinh,
                    // Lấy tồn kho hiện tại (hoặc 0 nếu chưa có dòng nào)
                    TonKho = _context.TonKho_CuaHangs
                        .Where(t => t.CuaHangID == storeId && t.SanPhamID == s.SanPhamID)
                        .Select(t => t.SoLuong)
                        .FirstOrDefault()
                })
                .ToList();

            ViewBag.ListSanPham = listSp;
            return View(new DeXuatViewModel());
        }

        // 5. CHI TIẾT
        public async Task<IActionResult> Details(int id)
        {
            var phieu = await _context.DeXuatNhapHangs
                .Include(d => d.ChiTietDeXuatNhaps).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DeXuatID == id);

            if (phieu == null) return NotFound();
            return View(phieu);
        }
    }
}