using Microsoft.Extensions.Logging;

namespace TmsApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}


public class EnrollmentService : IEnrollmentService
{
    private static readonly Dictionary<string, EnrollmentRecord> store = new();

    private readonly ILogger<EnrollmentService> logger;


    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        this.logger = logger;
    }


   public Task<EnrollmentRecord> EnrollAsync(
    string studentId,
    string courseCode)
{

    var existing = store.Values
        .FirstOrDefault(e =>
            e.StudentId == studentId &&
            e.CourseCode == courseCode);


    if (existing is not null)
    {
        logger.LogWarning(
            "Duplicate enrollment attempt {StudentId} already in {CourseCode} record {EnrollmentId}",
            studentId,
            courseCode,
            existing.Id
        );

        return Task.FromResult(existing);
    }



    var id = Guid.NewGuid()
        .ToString("N")[..8];


    var record = new EnrollmentRecord(
        id,
        studentId,
        courseCode,
        DateTime.UtcNow
    );


    store[id] = record;


    logger.LogInformation(
        "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
        studentId,
        courseCode,
        id
    );


    return Task.FromResult(record);
}


    public Task<EnrollmentRecord?> GetByIdAsync(string id)
{
    store.TryGetValue(id, out var record);


    if(record is null)
    {
        logger.LogWarning(
            "Enrollment {EnrollmentId} not found",
            id
        );
    }


    return Task.FromResult(record);
}


    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = store.Values.ToList();

        return Task.FromResult(all);
    }



    public Task<bool> DeleteAsync(string id)
{
    var removed = store.Remove(id);


    if(removed)
    {
        logger.LogInformation(
            "Deleted enrollment {EnrollmentId}",
            id
        );
    }
    else
    {
        logger.LogWarning(
            "Delete failed enrollment {EnrollmentId} not found",
            id
        );
    }


    return Task.FromResult(removed);
}

}



public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt
); 