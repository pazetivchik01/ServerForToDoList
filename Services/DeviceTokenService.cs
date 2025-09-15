using Microsoft.EntityFrameworkCore;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;

public class DeviceTokenService
{
    private readonly ToDoContext _context;
    public DeviceTokenService(ToDoContext context) => _context = context;

    public async Task<string> RegisterOrUpdateTokenAsync(DeviceRequest request, int userId)
    {
        var token = await _context.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.DeviceToken == request.Token && t.UserId == userId);

        if (token == null)
        {
            var newToken = new UserDeviceToken
            {
                DeviceType = request.Device,
                DeviceToken = request.Token,
                UserId = userId
            };
            await _context.UserDeviceTokens.AddAsync(newToken);
            await _context.SaveChangesAsync();
            return "registered";
        }
        else if (token.DeviceType != request.Device)
        {
            token.DeviceType = request.Device;
            var newDevice = new UserDeviceToken
            {
                DeviceType = token.DeviceType,
                DeviceToken = request.Token,
                UserId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.SaveChangesAsync();
            await _context.UserDeviceTokens.AddAsync(newDevice);
            return "add new device";
        }
            return "already up-to-date";
    }
}

