using TaskApi.Contracts;
using TaskApi.Models;

namespace TaskApi.Services
{
    public class RabbitMqService : IRabbitMqService
    {
        public async Task PublishTaskCompletedAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            // TODO: сериализует task в JSON, публикует в exchange task.events с routing key task.completed
        }
    }
}
