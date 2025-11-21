using BHX_Web.ViewModels; // Cần cho ErrorViewModel
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BHX_Web.Controllers
{
    // Controller này không có [Area]
    public class HomeController : Controller
    {
        // Trang chủ cho khách vãng lai
        public IActionResult Index()
        {
            return View();
        }

        // Trang giới thiệu
        public IActionResult About()
        {
            return View();
        }

        // Trang liên hệ
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}