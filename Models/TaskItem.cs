using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskApi.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; } = String.Empty; // не null, не пустой, максимум 200
        public bool IsCompleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; } // когда завершили
        public Priority Priority { get; set; } // Low, Medium, High
        [Timestamp]
        [JsonIgnore]
        public uint RowVersion { get; set; } // FIXME необходимо убрать из ответов сервера 
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }
}
