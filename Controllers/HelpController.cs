using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace ServerForToDoList.Controllers
{
    [ApiController]
    [Route("api/help")]
    public class HelpController : Controller
    {
        private readonly IWebHostEnvironment _env;

        private static readonly Counter HelpPageError = Metrics
        .CreateCounter("todo_task_created_total", "Total number of tasks created.",
            new CounterConfiguration
            {
                LabelNames = new[] { "error_type", "help_provider"}
            });

        public HelpController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [ResponseCache(Duration = 3600)]
        [HttpGet]
        public IActionResult GetHelp()
        {
            try
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Help", "Guide.html");
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Справка не найдена");
                    HelpPageError.WithLabels("help page not found", "GetHelp").Inc();
                }

                return PhysicalFile(filePath, "text/html");
            }
            catch (Exception ex)
            {
                HelpPageError.WithLabels("server error", "GetHelp").Inc();
                return StatusCode(500, "internal server error");
            }
        }
    }
}
