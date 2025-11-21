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
    public class TraHangController : Controller
    {
        private readonly BHXContext _context;

        public TraHangController(BHXContext context)
        {
            _context = context;
        }

        // Helper lấy ID Cửa hàng
        private int GetCurrentStoreId()
        {
            var claim = User.FindFirst("CuaHangID");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // 1. DANH SÁCH PHIẾU TRẢ (LỊCH SỬ)
        public async Task<IActionResult> Index()
        {
            int storeId = GetCurrentStoreId();
            var list = await _context.DanhSachTraHangs
                .Where(t => t.CuaHangID == storeId)
                .OrderByDescending(t => t.NgayTra)
                .ToListAsync();
            return View(list);
        }

        // 2. XEM CHI TIẾT
        public async Task<IActionResult> Details(int id)
        {
            var phieu = await _context.DanhSachTraHangs
                .Include(t => t.ChiTietTraHangs)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(t => t.TraHangID == id);

            if (phieu == null) return NotFound();
            return View(phieu);
        }

        // 3. TẠO PHIẾU TRẢ (GET)
        public IActionResult Create()
        {
            int storeId = GetCurrentStoreId();

            // Chỉ lấy những sản phẩm Cửa hàng ĐANG CÓ trong kho (SoLuong > 0)
            var listSp = _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId && t.SoLuong > 0)
                .Select(t => new
                {
                    t.SanPhamID,
                    TenHienThi = t.SanPham.TenSanPham + " (Đang có: " + t.SoLuong + ")",
                    TonKho = t.SoLuong
                })
                .ToList();

            ViewBag.ListSanPham = listSp;
            return View(new TraHangViewModel());
        }

        // 3. TẠO PHIẾU TRẢ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TraHangViewModel model)
        {
            int storeId = GetCurrentStoreId();

            // Lọc bỏ dòng rác (số lượng <= 0)
            var validItems = model.ChiTiets?.Where(x => x.SoLuong > 0).ToList();

            if (validItems != null && validItems.Any())
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // A. Tạo Phiếu Tổng
                        var phieuTra = new DanhSachTraHang
                        {
                            CuaHangID = storeId,
                            NgayTra = DateTime.Now,
                            TrangThai = "Chờ duyệt" // Gửi về chờ Admin xác nhận
                        };
                        _context.DanhSachTraHangs.Add(phieuTra);
                        await _context.SaveChangesAsync();

                        // B. Lưu Chi Tiết & Trừ Kho
                        foreach (var item in validItems)
                        {
                            // Kiểm tra tồn kho lần cuối cho chắc
                            var tonKho = await _context.TonKho_CuaHangs
                                .FirstOrDefaultAsync(t => t.CuaHangID == storeId && t.SanPhamID == item.SanPhamID);

                            if (tonKho == null || tonKho.SoLuong < item.SoLuong)
                            {
                                throw new Exception($"Sản phẩm ID {item.SanPhamID} không đủ số lượng để trả (Yêu cầu: {item.SoLuong}).");
                            }

                            // 1. Lưu chi tiết trả
                            var chiTiet = new ChiTietTraHang
                            {
                                TraHangID = phieuTra.TraHangID,
                                SanPhamID = item.SanPhamID,
                                SoLuong = item.SoLuong,
                                LyDo = item.LyDo
                            };
                            _context.ChiTietTraHangs.Add(chiTiet);

                            // 2. Trừ kho ngay lập tức (Hàng hỏng/hết hạn thì phải bỏ khỏi kho bán)
                            tonKho.SoLuong -= item.SoLuong;
                            _context.Update(tonKho);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Đã gửi phiếu trả hàng về Tổng công ty!";
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
                ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 sản phẩm.");
            }

            // Reload lại View nếu lỗi
            var listSp = _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId && t.SoLuong > 0)
                .Select(t => new { t.SanPhamID, TenHienThi = t.SanPham.TenSanPham + " (Đang có: " + t.SoLuong + ")", TonKho = t.SoLuong })
                .ToList();
            ViewBag.ListSanPham = listSp;

            return View(model);
        }
    }
}