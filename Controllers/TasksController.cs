using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerForToDoList.DBContext;
using ServerForToDoList.Repositories;
using System;
using System.Text.Json.Serialization;

namespace ServerForToDoList.Controllers
{
    [ApiController]
    [Route("api/task")]
    public class TasksController : ControllerBase
    {
        private readonly ToDoContext _context;

        public TasksController(ToDoContext context)
        {
            _context = context;
        }



        // GET api/task/get/'id'
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetTaskByTaskIdAsync(int id)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.TaskId == id);

                if (task == null)
                {
                    return NotFound($"Задача с ID {id} не найдена");
                }

                var taskDto = Extensions.TaskExtensions.ToDto(task);
                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }

        [HttpGet("created-by/{userId}")]
        public async Task<IActionResult> GetTasksCreatedByUserAsync(int userId)
        {

            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists) {
                    return NotFound($"Пользователь с ID {userId} не найден"); }

                var tasks = await _context.Tasks
                    .Include(t=>t.Assignments)
                    .Where(t => t.CreatedBy == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Ok(tasks.Select(t => Extensions.TaskExtensions.ToDto(t)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpGet("assigned-to/{userId}")]
        public async Task<IActionResult> GetTasksAssignedToUserAsync(int userId)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists) return NotFound($"Пользователь с ID {userId} не найден");

                var tasks = await _context.TaskAssignments
                .Where(ta => ta.UserId == userId)
                .Include(ta => ta.Task)  
                .ThenInclude(t => t.Assignments)  
                .Select(ta => ta.Task)
                    .Distinct()  
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

                
                var result = tasks.Select(t => Extensions.TaskExtensions.ToDto(t)).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST api/task
        [HttpPost]
        public async Task<IActionResult> CreateTaskAsync([FromBody] TaskDTO jsTask)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                Model.Task task = Extensions.TaskExtensions.ToEntity(jsTask);
                await _context.Tasks.AddAsync(task);
                await _context.SaveChangesAsync();

                TaskDTO responceTask = Extensions.TaskExtensions.ToDto(task);
                return CreatedAtAction(
                    actionName: nameof(GetTaskByTaskIdAsync),
                    routeValues: new { id = responceTask.TaskId },
                    value: responceTask
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }

        // PUT api/put/task/'id'
        [HttpPut("put/{id}")]
        public async Task<IActionResult> UpdateTaskAsync(int id, [FromBody] TaskDTO updatedTask)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingTask = await _context.Tasks
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.TaskId == id);
                if (existingTask == null)
                {
                    return NotFound();
                }

                existingTask.Title = updatedTask.Title;
                existingTask.Description = updatedTask.Description;
                existingTask.DueDate = updatedTask.DueDate;
                existingTask.DueTime = updatedTask.DueTime;
                existingTask.StartDate = updatedTask.StartDate;
                existingTask.IsImportant = updatedTask.IsImportant;
                existingTask.TypeId = updatedTask.TypeId;
                existingTask.Status = updatedTask.Status;
                existingTask.CompletedAt = updatedTask.CompletedAt;
                existingTask.IsConfirmed = updatedTask.IsConfirmed;

                if (updatedTask.Assignments != null&&updatedTask.Assignments.Count!=0) 
                {
                    await TaskAssignmentRepository.ProcessTaskAssignments( _context,existingTask, updatedTask.Assignments);
                }


                await _context.SaveChangesAsync();

                // Перезагружаем задачу с актуальными данными (включая назначения)
                var refreshedTask = await _context.Tasks
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.TaskId == id);

                return Ok(Extensions.TaskExtensions.ToDto(refreshedTask));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }
    }

    public class TaskDTO
    {
        [JsonPropertyName("task_id")]
        public int TaskId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("due_time")]
        public TimeSpan? DueTime { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("is_important")]
        public bool IsImportant { get; set; }

        [JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public int CreatedBy { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("is_confirmed")]
        public bool IsConfirmed { get; set; }

        public List<TaskAssignmentsDTO>? Assignments { get; set; }

    }
    public class TaskAssignmentsDTO
    {
        [JsonPropertyName("assignment_id")]
        public int? AssignmentId { get; set; }

        [JsonPropertyName("task_id")]
        public int? TaskId { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("assigned_at")]
        public DateTime? AssignedAt { get; set; }

        [JsonPropertyName("assigned_by")]
        public int? AssignedBy { get; set; }

        [JsonPropertyName("to_delete")]
        public bool ToDelete { get; set; } = false;
    }
}
