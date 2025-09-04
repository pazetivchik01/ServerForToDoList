using Microsoft.EntityFrameworkCore;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;

namespace ServerForToDoList.Repositories
{
    public class UserRepository
    {
        // Хранилище методов для работы с user (CRUD create read update delete)


        #region create
        //Добавление пользователя
        public static async System.Threading.Tasks.Task AddUserAsync(ToDoContext context, User user)
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }
        #endregion

        #region read
        //Получение пользователя по id
        public static async Task<User?> GetUserByIdAsync(ToDoContext context, int userId)
        {
            return await context.Users.FirstOrDefaultAsync(x => x.UserId == userId); // Возвращает null если не найден
        }

        //Получение всех пользователей(Для админа у которого created_by=null)
        public static async Task<List<User>> GetUsersAsync(ToDoContext context)
        {
            return await context.Users.Where(u => u.CreatedBy != null).ToListAsync();
        }

        //Получение пользователе по id создателя
        public static async Task<List<User>> GetAllUserByIdCreatedAsync(ToDoContext context, int creatorId)
        {
            var result = new List<User>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(creatorId);

            var creator = await context.Users.FirstOrDefaultAsync(u => u.UserId == creatorId);
            if (creator != null)
            {
                result.Add(creator);
                visited.Add(creatorId);
            }

            while (queue.Count > 0)
            {
                var currentCreatorId = queue.Dequeue();

                var directUsers = await context.Users
                    .Where(u => u.CreatedBy == currentCreatorId)
                    .ToListAsync();

                foreach (var user in directUsers)
                {
                    if (visited.Add(user.UserId)) 
                    {
                        result.Add(user);
                        queue.Enqueue(user.UserId);
                    }
                }
            }

            return result;
        }

        public static async Task<List<User>> GetAllUserForManageAsync(ToDoContext context, int creatorId)
        {
            var result = new List<User>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(creatorId);

            var creator = await context.Users.FirstOrDefaultAsync(u => u.UserId == creatorId);
            if (creator != null)
            {
                result.Add(creator);
                visited.Add(creatorId);
            }

            while (queue.Count > 0)
            {
                var currentCreatorId = queue.Dequeue();

                var directUsers = await context.Users
                    .Where(u => u.CreatedBy == currentCreatorId)
                    .ToListAsync();

                foreach (var user in directUsers)
                {
                    if (visited.Add(user.UserId))
                    {
                        result.Add(user);
                        queue.Enqueue(user.UserId);
                    }
                }
            }

            return result.Where(x => x.DeletedAt == null).ToList();
        }
        #endregion

        #region update
        //Редактирование пользователя
        public static async System.Threading.Tasks.Task UpdateUserAsync(ToDoContext context, User user)
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
            
        }
        #endregion

        #region delete
        //Удаление пользователя(полностью)
        public static async void DeleteUserAsync(ToDoContext context, int userId)
        {
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }

        //Удаление пользователя(soft-delte)
        public static async System.Threading.Tasks.Task SoftDeleteUserAsync(ToDoContext context, int userId)
        {
           var user = await context.Users.FindAsync(userId);
           user.DeletedAt = DateTime.Now;
           await context.SaveChangesAsync();
        }
        #endregion
        
    }

}
