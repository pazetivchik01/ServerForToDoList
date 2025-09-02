using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerForToDoList.Controllers;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly ToDoContext _context;
    private readonly DeviceTokenService _deviceService;
    public DeviceController(ToDoContext context, DeviceTokenService deviceService)
    {
        _context = context;
        _deviceService = deviceService;
    }
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
            return BadRequest("Token is required");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await _deviceService.RegisterOrUpdateTokenAsync(request, int.Parse(userIdClaim));
        return Ok($"{result}: Device {request.Device}, token {request.Token}");
    }
    [HttpDelete("delete")] // http://localhost:5131/api/device/delete
    public IActionResult delete_token([FromBody] DeviceRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
            return BadRequest("Token is required");

        return Ok($"Token: {request.Token}, device: {request.Device}, succefuly deleted"); // удаление токена
    }

}

public class DeviceRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
    [JsonPropertyName("device")]
    public string Device { get; set; }
}