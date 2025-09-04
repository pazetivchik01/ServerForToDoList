using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;
using ServerForToDoList.Repositories;
using System;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace ServerForToDoList.Controllers
{
    [ApiController]
    [Route("api/task")]
    public class TasksController : ControllerBase
    {
        private readonly ToDoContext _context;
        private readonly FcmNotificationService _notificationService;
        private readonly ILogger<AuthController> _logger;

        public TasksController(ToDoContext context, FcmNotificationService notificationService, ILogger<AuthController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }


        // GET api/task/taskId
        [HttpGet("{id}")]
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

        //Get api/task/created-by
        [Authorize(Roles = "admin,manager")]
        [HttpGet("created-by")]
        public async Task<IActionResult> GetTasksCreatedByUserAsync()
        {

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) throw new Exception();
                var userExists = await _context.Users.AnyAsync(u => u.UserId == int.Parse(userId.ToString()));
                if (!userExists)
                {
                    return NotFound($"Пользователь с ID {userId.ToString()} не найден");
                }

                var tasks = await _context.Tasks
                    .Include(t => t.Assignments)
                    .Where(t => t.CreatedBy == int.Parse(userId.ToString()))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Ok(tasks.Select(t => Extensions.TaskExtensions.ToDto(t)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        //Get api/task/assigned-to
        [Authorize]
        [HttpGet("assigned-to")]
        public async Task<IActionResult> GetTasksAssignedToUserAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) throw new Exception();
                var userExists = await _context.Users.AnyAsync(u => u.UserId == int.Parse(userId.ToString()));
                if (!userExists) return NotFound($"Пользователь с ID {userId} не найден");

                var tasks = await _context.TaskAssignments
                .Where(ta => ta.UserId == int.Parse(userId.ToString()))
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

        //Get api/task/assigned-to/id
        [Authorize]
        [HttpGet("assigned-to/{userId}")]
        public async Task<IActionResult> GetTasksAssignedToUserAsync(int userId)
        {
            try
            {
                if (userId == null) throw new Exception();
                var userExists = await _context.Users.AnyAsync(u => u.UserId == int.Parse(userId.ToString()));
                if (!userExists) return NotFound($"Пользователь с ID {userId} не найден");

                var tasks = await _context.TaskAssignments
                .Where(ta => ta.UserId == int.Parse(userId.ToString()))
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
        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<IActionResult> CreateTaskAsync([FromBody] TaskDTO jsTask)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                Model.Task task = Extensions.TaskExtensions.ToEntity(jsTask);
                task.CreatedBy = int.Parse(userIdClaim);

                task.CreatedAt = DateTime.UtcNow;
                if (task.Assignments != null)
                {


                    foreach (var assignment in task.Assignments)
                    {
                        assignment.AssignedAt = DateTime.UtcNow;
                        assignment.AssignedBy = int.Parse(userIdClaim);
                    }


                }
                await _context.Tasks.AddAsync(task);
                await _context.SaveChangesAsync();

                var deviceTokens = await _context.UserDeviceTokens
                     .Where(u => u.UserId == int.Parse(userIdClaim))
                     .Select(u => u.DeviceToken)
                     .ToListAsync();

                foreach (var token in deviceTokens)
                {
                    await _notificationService.SendNotificationAsync(token, "Новая задача", $"Назначена новая задача: {jsTask.Title}");
                }

                TaskDTO responceTask = Extensions.TaskExtensions.ToDto(task);

                return Ok(responceTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }

        // PUT api/task
        [Authorize(Roles = "admin,manager")]
        [HttpPut]
        public async Task<IActionResult> UpdateTaskAsync( [FromBody] TaskDTO updatedTask)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingTask = await _context.Tasks
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.TaskId == updatedTask.TaskId);
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

                if (updatedTask.Assignments != null && updatedTask.Assignments.Count != 0)
                {
                    await TaskAssignmentRepository.ProcessTaskAssignments(_context, existingTask, updatedTask.Assignments);
                }

                await _context.SaveChangesAsync();

                // Перезагружаем задачу с актуальными данными (включая назначения)
                var refreshedTask = await _context.Tasks
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.TaskId == updatedTask.TaskId);

                List<Model.TaskAssignment> oldTaskAssignments = (List<Model.TaskAssignment>)existingTask.Assignments;
                List<Model.TaskAssignment> newTaskAssignments = (List<Model.TaskAssignment>)refreshedTask.Assignments;

                var unionAssignments = oldTaskAssignments.Union(newTaskAssignments).ToList();

                if (unionAssignments.Count != 0)
                {
                    List<int> userId = new List<int>();
                    for (int i = 0; i < unionAssignments.Count; i++)
                    {
                        userId.Add(unionAssignments[i].UserId);
                    }
                    var deviceTokens = new List<string>();
                    foreach (var id in userId)
                    {
                        deviceTokens = await _context.UserDeviceTokens
                         .Where(u => u.UserId == id)
                         .Select(u => u.DeviceToken)
                         .ToListAsync();
                    }
                    foreach (var token in deviceTokens)
                    {
                        await _notificationService.SendNotificationAsync(token, "Новая задача", $"Вас добавили в новую задачу: {refreshedTask.Title}");
                    }
                }
                return Ok(Extensions.TaskExtensions.ToDto(refreshedTask));
            }
            catch(TokenResponseException ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode(500, "Произошла внутренняя ошибка сервера: " + ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }

        // Patch api/task/confirmed/id
        [Authorize]
        [HttpPatch("confirmed/{taskId}")]
        public async Task<IActionResult> ConfirmedTaskAsync([FromBody] bool flag, int taskId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    return NotFound($"Задача с ID {taskId} не найдена.");
                }


                var Task = await _context.Tasks
                  .Include(t => t.Assignments)
                  .FirstOrDefaultAsync(t => t.TaskId == taskId);
                List<TaskAssignment> assignment = (List<TaskAssignment>)Task.Assignments;
                if (flag)
                {
                    int? id = assignment[0].AssignedBy;
                    if (id != null)
                    {
                        var deviceTokens = new List<string>();
                        deviceTokens = await _context.UserDeviceTokens
                             .Where(u => u.UserId == id)
                             .Select(u => u.DeviceToken)
                             .ToListAsync();
                        foreach (var token in deviceTokens)
                        {
                            await _notificationService.SendNotificationAsync(token, "Задача на проверку", $"Появилась задача на проверку");
                        }
                    }
                }
                else
                {
                    List<int> id = new List<int>();
                    foreach (var id_employee in assignment)
                    {
                        id.Add(id_employee.UserId);
                    }
                    if (!id.IsNullOrEmpty())
                    {
                        var deviceTokens = new List<string>();
                        foreach (var idd in id)
                        {
                            deviceTokens = await _context.UserDeviceTokens
                             .Where(u => u.UserId == idd)
                             .Select(u => u.DeviceToken)
                             .ToListAsync();
                        }
                        foreach (var token in deviceTokens)
                        {
                            await _notificationService.SendNotificationAsync(token, "Задача не прошла проверку", $"Ваша задача {Task.Title} не прошла проверку и возвращена в работу");
                        }
                    }
                }

                task.IsConfirmed = flag;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }

        // Patch api/task/status/id
        [Authorize(Roles = "admin,manager")]
        [HttpPatch("status/{taskId}")]
        public async Task<IActionResult> ComplitedTaskAsync(int taskId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    return NotFound($"Задача с ID {taskId} не найдена.");
                }
                if (!task.IsConfirmed)
                {
                    return UnprocessableEntity($"Нельзя сделать задачу выполненной, если она не завершенна исполнителем.");
                }
                task.Status = true;
                task.CompletedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла внутренняя ошибка сервера");
            }
        }

        // Delete api/task/id
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    return NotFound($"Задача с ID {taskId} не найдена.");
                }
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                return NoContent();
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