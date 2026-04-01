using DocumentProcessing.Worker.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentProcessing.Worker.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddHostedService<DocumentJobConsumer>();
        return services;
    }
}