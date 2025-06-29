using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServerForToDoList.Controllers
{
    [ApiController]
    [Route("api/help")]
    public class HelpController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public HelpController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [ResponseCache(Duration = 3600)]
        [HttpGet]
        public IActionResult GetHelp()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Help", "Guide.html");
            if (!System.IO.File.Exists(filePath))
                return NotFound("Справка не найдена");

            return PhysicalFile(filePath, "text/html");
        }
    }
}
