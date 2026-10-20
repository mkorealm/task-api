using Microsoft.EntityFrameworkCore;
using TaskApi.Contracts;
using TaskApi.Infrastructure;
using TaskApi.Models;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);

#region Services Registration

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TaskDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

#endregion

var app = builder.Build();

#region Database init

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    await db.Database.EnsureCreatedAsync();
}

#endregion

#region Endpoints

app.MapPost("/tasks", async (string title, Priority? priority, TaskDbContext db, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        return Results.BadRequest( new { error = "Введённый title некорректен!" });

    var task = new TaskItem
    {
        Id = Guid.NewGuid(),
        Title = title.Trim(),
        IsCompleted = false,
        CreatedAt = DateTimeOffset.UtcNow,
        CompletedAt = null,
        Priority = priority ?? Priority.Medium
    };

    await db.Tasks.AddAsync(task, cancellationToken);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/tasks/{task.Id}", task); 
});

app.MapGet("/tasks", async (TaskDbContext db, CancellationToken cancellationToken) =>
{
    return await db.Tasks.AsNoTracking().ToListAsync(cancellationToken);
});

app.MapPut("/tasks/{id:guid}/complete", async (Guid id, TaskDbContext db, RabbitMqService rabbit, CancellationToken cancellationToken) =>
{
    TaskItem task = await db.Tasks.FindAsync(id, cancellationToken);
    if (task is null)
        return Results.NotFound();
    if (task.IsCompleted)
        return Results.Conflict(new { error = "Задание уже завершено!" });

    task.IsCompleted = true;
    task.CompletedAt = DateTimeOffset.UtcNow;

    try
    {
        await db.SaveChangesAsync(cancellationToken);
        _ = Task.Run(async () =>
        {
            try { await rabbit.PublishTaskCompletedAsync(task); }
            catch (Exception ex) { /*_logger.LogWarning(ex, "RabbitMQ ошибка публикации");*/ }
        });
    }
    catch (DbUpdateConcurrencyException)
    {
        return Results.Conflict(new { error = "Задание было изменено другим запросом." });
    }

    return Results.Ok(task);
});

app.MapDelete("/tasks/{id:guid}", async (Guid id, TaskDbContext db, CancellationToken cancellationToken) =>
{
    TaskItem task = await db.Tasks.FindAsync(id, cancellationToken);
    if (task is null)
        return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(task);
});

#endregion

#region Middleware Pipeline

// Swagger для удобства
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

#endregion

app.Run();