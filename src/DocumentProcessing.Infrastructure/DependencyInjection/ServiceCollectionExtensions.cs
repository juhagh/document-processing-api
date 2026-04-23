using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Infrastructure.Messaging;
using DocumentProcessing.Infrastructure.Persistence;
using DocumentProcessing.Infrastructure.Persistence.UnitOfWork;
using DocumentProcessing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DocumentProcessing.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DocumentProcessingDatabase") 
                               ?? throw new InvalidOperationException("Connection string 'DocumentProcessingDatabase' was not found.");

        services.AddDbContext<DocumentProcessingDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection("RabbitMq"))
            .Validate(o =>
                    !string.IsNullOrWhiteSpace(o.HostName) &&
                    !string.IsNullOrWhiteSpace(o.UserName) &&
                    !string.IsNullOrWhiteSpace(o.Password) &&
                    !string.IsNullOrWhiteSpace(o.VirtualHost)&&
                    !string.IsNullOrWhiteSpace(o.ClientName)&&
                    !string.IsNullOrWhiteSpace(o.QueueName),
                "RabbitMQ configuration is missing required fields.")
            .ValidateOnStart();
        
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return new ConnectionFactory
            {
                HostName = options.HostName,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                Port = options.Port,
                ClientProvidedName = options.ClientName,
                AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                RequestedHeartbeat = TimeSpan.FromSeconds(options.RequestedHeartbeat),
            };
        });

        services.AddScoped<IJobMessagePublisher, RabbitMqJobMessagePublisher>();

        return services;
    }
}