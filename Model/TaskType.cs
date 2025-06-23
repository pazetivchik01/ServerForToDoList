using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ServerForToDoList.Model
{
    [Table("task_types")]
    public class TaskType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("type_id")]
        public int TypeId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("type_name")]
        public string TypeName { get; set; }

        [Column("is_accessible")]
        public bool IsAccessible { get; set; } = true;

        // Навигационное свойство
        public ICollection<Task> Tasks { get; set; }
    }
}
