// Controllers/TestApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sistema_GuiaLocal_Turismo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TestApiController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                success = true,
                message = "API funcionando correctamente",
                timestamp = DateTime.Now
            });
        }
    }
}