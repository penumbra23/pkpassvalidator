using Microsoft.AspNetCore.Mvc;

namespace PassValidator.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Health()
        {
            return Ok("{\"health\":\"ok\"}");
        }
    }
}