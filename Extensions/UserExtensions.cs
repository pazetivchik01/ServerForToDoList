using ServerForToDoList.Model;

namespace ServerForToDoList.Extensions
{
    public class UserExtensions
    {
        public static User ConvertToUser(UserRequest request)
        {
            if (request == null)
                return null;

            return new User
            {
                UserId = request.id,
                LastName = request.lastName,
                FirstName = request.firstName,
                Surname = request.surname,
                Login = request.login,
                PasswordHash = request.password,
                Role = request.role,
                CreatedBy = request.createdBy,
                // Остальные поля User инициализируются по умолчанию
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null,
                CreatedTasks = new List<Model.Task>(),
                Assignments = new List<TaskAssignment>(),
                DeviceTokens = new List<UserDeviceToken>()
            };
        }

        public static UserRequest ConvertToUserRequest(User user)
        {
            if (user == null)
                return null;

            return new UserRequest
            {
                id = user.UserId,
                lastName = user.LastName,
                firstName = user.FirstName,
                surname = user.Surname,
                login = user.Login,
                password = user.PasswordHash,
                role = user.Role,
                createdBy = user.CreatedBy
            };
        }
    }
}
