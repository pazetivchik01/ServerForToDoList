using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ToDoContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ToDoContext context,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == request.login);

            if (user == null)
            {
                _logger.LogWarning($"Попытка входа с несуществующим логином: {request.login}");
                var mes = new { Message = "Неверные учетные данные" };
                return Unauthorized(mes);
            }



            if (!BCrypt.Net.BCrypt.Verify(request.password, user.PasswordHash))
            {
                _logger.LogWarning($"Неверный пароль для пользователя: {request.login}");
                return Unauthorized(new { Message = "Неверные учетные данные" });
            }

            var token = GenerateJwtToken(user);
            var answer = new
            {
                Token = token,
                User = new
                {
                    user.UserId,
                    user.Login,
                    user.Role,
                    user.FirstName,
                    user.LastName
                }
            };
            return Ok(answer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при авторизации");
            return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}