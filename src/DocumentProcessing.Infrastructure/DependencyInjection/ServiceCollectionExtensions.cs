using DocumentProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}