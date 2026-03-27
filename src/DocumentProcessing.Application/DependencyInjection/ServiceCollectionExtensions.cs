using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentProcessing.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IJobService, JobService>();

        return services;
    }
}