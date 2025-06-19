using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;
using ServerForToDoList.Repositories;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    //Êîðî÷å, â program.cs çàäàåòñÿ êîíòåêñò ÁÄ, è îí, êàê ÿ ïîíÿë àâòîìàòè÷åñêè âñòðàèâàåòñÿ â êîíòðîëëû. Íî ÷òî áû èì ïîëüçîâàòüñÿ åãî íàäî ÿâíî îáúÿâèòü
    private readonly ToDoContext _context;

    public UserController(ToDoContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { Message = "ID must be a positive integer" });

            var user = await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.Surname,
                    u.FirstName,
                    u.LastName,
                    u.Login,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { Message = $"User with id {id} not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Произошла внутренняя ошибка сервера");
        }
    }

    [HttpGet("getAll")] // http://localhost:5131/api/user/getAll 
    public IActionResult GetAllUser()
    {
        try 
        {
            // Ëîãèêà ïîëó÷åíèÿ ïîëüçîâàòåëåé
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest("An unknown error occurred");
        }
    }
    [HttpPost("register")] // http://localhost:5131/api/user/register
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateUser([FromBody] UserRequest user)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Login == user.login))
            {
                return Conflict(new { Message = "Пользователь с таким логином уже существует" });
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var newUser = new User
            {
                UserId = user.id,
                LastName = user.lastName,
                FirstName = user.firstName,
                Surname = user.surname,
                Login = user.login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.password),
                Role = user.role,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = int.Parse(userIdClaim.ToString())
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Возвращаем 201 Created с URL в заголовке Location
            var answer = new
            {
                surname = newUser.Surname,
                first_name = newUser.FirstName,
                message = "Пользователь успешно создан"
            };


            return Created($"/api/user/get/{newUser.UserId}", answer);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Произошла внутренняя ошибка сервера");
        }
    }
    [HttpPut("update")] // http://localhost:5131/api/user/update
    public IActionResult update_user([FromBody] UserRequest request)
    {
        if (request == null)
            return BadRequest("Error accepting data, data is null");

        return Ok($"User: {request.lastName} {request.firstName} {request.surname} succefuly updated"); // update user
    }
    [HttpDelete("delete")] // http://localhost:5131/api/user/delete
    public IActionResult delete_user([FromBody] UserRequest request)
    {
        if (request == null)
            return BadRequest("Error accepting data, data is null");

        return Ok($"User: {request.lastName} {request.firstName} {request.surname} succefuly deleted"); // delete user
    }
}

public class UserRequest // json îòïðàâëÿòü â ñîîòâåòñòâèè ñ ïîðäêîì ïîëåé â êëàññå
{
    [JsonPropertyName("user_id")]
    public int id { get; set; }
    [JsonPropertyName("last_name")]
    public string? lastName { get; set; }
    [JsonPropertyName("first_name")]
    public string firstName { get; set; }
    [JsonPropertyName("surname")]
    public string surname { get; set; }
    [JsonPropertyName("login")]
    public string login { get; set; }
    [JsonPropertyName("password_hash")]
    public string password { get; set; }
    [JsonPropertyName("role")]
    public string role { get; set; }
    [JsonPropertyName("created_by")]
    public int? createdBy { get; set; }
}