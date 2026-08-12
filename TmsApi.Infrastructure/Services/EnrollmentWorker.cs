using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        using var scope = scopeFactory.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IEnrollmentService>();
    }
}