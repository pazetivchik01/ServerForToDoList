using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;

namespace ServerForToDoList.Controllers
{
    [ApiController]
    [Route("api/tasktype")]
    public class TaskTypeController : Controller
    {
        private readonly ToDoContext _context;

        public TaskTypeController(ToDoContext context)
        {
            _context = context;
        }

        // GET api/tasktype
        [HttpGet]
        public async Task<ActionResult<List<TaskTypeDto>>> GetAll()
        {
            return await _context.TaskTypes
                .Select(t => new TaskTypeDto
                {
                    TypeId = t.TypeId,
                    TypeName = t.TypeName,
                    IsAccessible = t.IsAccessible
                })
                .AsNoTracking()
                .ToListAsync();
        }

        // POST api/tasktype
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return BadRequest("Название типа обязательно");

            string normalized = typeName.Trim().ToLower();
            bool exists = await _context.TaskTypes
                .AnyAsync(t => t.TypeName.Trim().ToLower() == normalized);

            if (exists)
                return Conflict("Тип задачи уже существует");

            _context.TaskTypes.Add(new TaskType
            {
                TypeName = typeName.Trim(),
                IsAccessible = true
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT api/tasktype/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] string newTypeName)
        {
            if (string.IsNullOrWhiteSpace(newTypeName))
                return BadRequest("Название типа обязательно");

            var type = await _context.TaskTypes.FindAsync(id);
            if (type == null)
                return NotFound();

            string normalized = newTypeName.Trim().ToLower();
            bool exists = await _context.TaskTypes
                .AnyAsync(t => t.TypeId != id && t.TypeName.Trim().ToLower() == normalized);

            if (exists)
                return Conflict("Название типа задачи уже занято");

            type.TypeName = newTypeName.Trim();
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH api/tasktype/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateAccessibility(int id, [FromBody] bool isAccessible)
        {
            var type = await _context.TaskTypes.FindAsync(id);
            if (type == null)
                return NotFound();

            type.IsAccessible = isAccessible;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class TaskTypeDto
    {
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public bool IsAccessible { get; set; }
    }
}
