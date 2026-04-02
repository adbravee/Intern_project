using Microsoft.AspNetCore.Mvc;

namespace ItemProcessingApp.Controllers
{
    public class HomeController : Controller
    {
        [Route("Home/Error")]
        public IActionResult Error() => View();
    }
}
