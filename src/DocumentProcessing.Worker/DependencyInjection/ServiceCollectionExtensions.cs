using DocumentProcessing.Worker.Consumers;
using DocumentProcessing.Worker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentProcessing.Worker.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentAnalysisService,DocumentAnalysisService>();
        services.AddHostedService<DocumentJobConsumer>();
        
        return services;
    }
}