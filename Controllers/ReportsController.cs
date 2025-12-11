// Controllers/ReportsController.cs
using Microsoft.AspNetCore.Mvc;

namespace CostaRicaTourismSystem.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}