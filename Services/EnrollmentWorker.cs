namespace TmsApi.Services;


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


        var enrollments = service.GetAllAsync();


    }

}