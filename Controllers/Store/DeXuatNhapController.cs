using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;
using BHX_Web.ViewModels;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")] // <--- QUAN TRỌNG: Định danh khu vực Store
    [Authorize(Roles = "Store")] // <--- QUAN TRỌNG: Chỉ tài khoản Store mới vào được
    public class DeXuatNhapController : Controller
    {
        private readonly BHXContext _context;

        public DeXuatNhapController(BHXContext context)
        {
            _context = context;
        }

        // Helper: Lấy ID Cửa hàng từ Cookie đăng nhập
        private int GetCurrentStoreId()
        {
            var claim = User.FindFirst("CuaHangID");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // ============================================================
        // 1. DANH SÁCH LỊCH SỬ ĐỀ XUẤT (INDEX)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            int storeId = GetCurrentStoreId();
            if (storeId == 0) return RedirectToAction("Login", "Account", new { area = "" });

            var list = await _context.DeXuatNhapHangs
                .Where(d => d.CuaHangID == storeId)
                .OrderByDescending(d => d.NgayDeXuat)
                .ToListAsync();

            return View(list);
        }

        // ============================================================
        // 2. TẠO ĐỀ XUẤT TỰ ĐỘNG (GET)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> AutoSuggest()
        {
            int storeId = GetCurrentStoreId();

            // 1. Quét kho: Tìm sản phẩm có SoLuong < 10
            var lowStockItems = await _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId && t.SoLuong < 10)
                .ToListAsync();

            // 2. Đổ dữ liệu vào ViewModel
            var model = new DeXuatViewModel();
            foreach (var item in lowStockItems)
            {
                if (item.SanPham != null) // Kiểm tra null an toàn
                {
                    model.Items.Add(new DeXuatItem
                    {
                        SanPhamID = item.SanPhamID,
                        TenSanPham = item.SanPham.TenSanPham,
                        DonViTinh = item.SanPham.DonViTinh,
                        HinhAnh = item.SanPham.HinhAnh,
                        TonKhoHienTai = item.SoLuong,
                        SoLuongDeXuat = 50 // Gợi ý nhập thêm 50 (User có thể sửa)
                    });
                }
            }

            return View(model);
        }

        // ============================================================
        // 3. GỬI YÊU CẦU (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeXuatViewModel model)
        {
            int storeId = GetCurrentStoreId();

            // Lọc ra những dòng có số lượng nhập > 0
            var validItems = model.Items?.Where(x => x.SoLuongDeXuat > 0).ToList();

            if (validItems != null && validItems.Any())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // A. Tạo Phiếu Đề Xuất
                        var deXuat = new DeXuatNhapHang
                        {
                            CuaHangID = storeId,
                            NgayDeXuat = DateTime.Now,
                            TrangThai = "Chờ duyệt"
                        };
                        _context.DeXuatNhapHangs.Add(deXuat);
                        await _context.SaveChangesAsync(); // Lưu để lấy ID

                        // B. Lưu Chi Tiết
                        foreach (var item in validItems)
                        {
                            var chiTiet = new ChiTietDeXuatNhap
                            {
                                DeXuatID = deXuat.DeXuatID,
                                SanPhamID = item.SanPhamID,
                                SoLuong = item.SoLuongDeXuat
                            };
                            _context.ChiTietDeXuatNhaps.Add(chiTiet);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Đã gửi yêu cầu thành công! Vui lòng chờ Tổng công ty duyệt.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng nhập số lượng cho ít nhất 1 sản phẩm.");
            }

            return View("AutoSuggest", model);
        }

        // ============================================================
        // 4. XEM CHI TIẾT PHIẾU (DETAILS)
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            var phieu = await _context.DeXuatNhapHangs
                .Include(d => d.ChiTietDeXuatNhaps)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.DeXuatID == id);

            if (phieu == null) return NotFound();

            return View(phieu);
        }
        [HttpGet]
        public IActionResult CreateManual()
        {
            // Lấy danh sách tất cả sản phẩm để hiển thị Dropdown
            // Kèm theo tồn kho hiện tại để tham khảo
            int storeId = GetCurrentStoreId();

            var listSp = _context.SanPhams
                .Select(s => new
                {
                    s.SanPhamID,
                    TenHienThi = s.TenSanPham,
                    s.DonViTinh,
                    // Lấy tồn kho hiện tại của cửa hàng này (nếu chưa có thì = 0)
                    TonKho = _context.TonKho_CuaHangs
                                .Where(t => t.CuaHangID == storeId && t.SanPhamID == s.SanPhamID)
                                .Select(t => t.SoLuong)
                                .FirstOrDefault()
                })
                .ToList();

            ViewBag.ListSanPham = listSp;

            return View(new DeXuatViewModel());
        }
    }

}