using TaskApi.Models;

namespace TaskApi.Contracts
{
    public interface IRabbitMqService
    {
        Task PublishTaskCompletedAsync(TaskItem task, CancellationToken cancellationToken = default);
    }
}
