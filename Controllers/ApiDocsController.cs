// Controllers/ApiDocsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sistema_GuiaLocal_Turismo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ApiDocsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}  
