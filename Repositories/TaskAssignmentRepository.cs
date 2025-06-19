using Microsoft.EntityFrameworkCore;
using ServerForToDoList.Controllers;
using ServerForToDoList.DBContext;
using ServerForToDoList.Model;

namespace ServerForToDoList.Repositories
{
    public  class TaskAssignmentRepository
    {

        public static async System.Threading.Tasks.Task ProcessTaskAssignments(
        ToDoContext context,
        Model.Task existingTask,
        List<TaskAssignmentsDTO> assignmentDTOs)
        {
            var assignmentsToDelete = assignmentDTOs
                .Where(dto => dto.ToDelete && dto.AssignmentId.HasValue)
                .Select(dto => dto.AssignmentId.Value)
                .ToList();

            if (assignmentsToDelete.Any())
            {
                var existingAssignmentsToDelete = await context.TaskAssignments
                    .Where(a => assignmentsToDelete.Contains(a.AssignmentId))
                    .ToListAsync();

                context.TaskAssignments.RemoveRange(existingAssignmentsToDelete);
            }

            var newAssignments = assignmentDTOs
                .Where(dto => !dto.ToDelete && (!dto.AssignmentId.HasValue || dto.AssignmentId == 0))
                .Select(dto =>Extensions.TaskAssignmentExtensions.ToEntity(dto))
                .Where(a => a != null)
                .ToList();

            foreach (var assignment in newAssignments)
            {
                assignment.TaskId = existingTask.TaskId;
            }

            var userIds = newAssignments
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            var existingUserIds = await context.Users
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => u.UserId)
                .ToListAsync();

            var missingUserIds = userIds.Except(existingUserIds).ToList();
            if (missingUserIds.Any())
            {
                throw new ArgumentException($"Следующие UserId не существуют: {string.Join(", ", missingUserIds)}");
            }

            if (newAssignments.Any())
            {
                await context.TaskAssignments.AddRangeAsync(newAssignments);
            }
        }



    }
}
