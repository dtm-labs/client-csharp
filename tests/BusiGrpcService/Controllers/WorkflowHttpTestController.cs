using Microsoft.AspNetCore.Mvc;

namespace BusiGrpcService.Controllers
{
    [ApiController]
    public class WorkflowHttpTestController : ControllerBase
    {
        [HttpGet("test-http-ok1")]
        public IActionResult TestHttpOk1()
        {
            Console.Out.WriteLine($"QueryString: {Request.QueryString}");
            return Content("SUCCESS");
        }

        [HttpGet("test-http-ok2")]
        public IActionResult TestHttpOk2()
        {
            Console.Out.WriteLine($"QueryString: {Request.QueryString}");
            return Content("SUCCESS");
        }

        [HttpGet("409")]
        public IActionResult Test409()
        {
            Console.Out.WriteLine($"QueryString: {Request.QueryString}");
            Response.StatusCode = 409;
            return Content("i am body, the http branch is 409");
        }
    }
}