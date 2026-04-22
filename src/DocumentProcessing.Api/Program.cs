using System.Text.Json.Serialization;
using DocumentProcessing.Api.BackgroundServices;
using DocumentProcessing.Application.DependencyInjection;
using DocumentProcessing.Infrastructure.DependencyInjection;
using DocumentProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection("OutboxOptions"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentProcessingDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();