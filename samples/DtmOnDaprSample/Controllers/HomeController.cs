using Microsoft.AspNetCore.Mvc;

namespace DtmSample.Controllers
{
    public class HomeController : ControllerBase
    {
        [HttpGet("")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index()
        {
            return Redirect("~/swagger");
        }
    }
}
