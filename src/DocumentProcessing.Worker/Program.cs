using DocumentProcessing.Application.DependencyInjection;
using DocumentProcessing.Infrastructure.DependencyInjection;
using DocumentProcessing.Worker.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorkerServices();

var host = builder.Build();
await host.RunAsync();