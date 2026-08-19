using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(
    TmsDbContext context,
    ILogger<EnrollmentService> logger)
    : IEnrollmentService
{
    public async Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId && e.Id == id)
            .Select(e => new EnrollmentResponseDto(
               e.Id,
e.CourseId,
e.StudentId,
e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var enrollment = new Enrollment
{
    StudentId = request.StudentId,
    CourseId = courseId,
    EnrolledAt = DateTime.UtcNow
};
        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created enrollment {EnrollmentId} for student {StudentId} in course {CourseId}",
            enrollment.Id,
            enrollment.StudentId,
            enrollment.CourseId);

        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
    e.Id,
    e.CourseId,
    e.StudentId,
    e.EnrolledAt))
            .ToListAsync(ct);
    }

public async Task<bool> ExistsAsync(
    int studentId,
    string courseCode,
    CancellationToken ct)
{
    return await context.Enrollments
        .AnyAsync(
            e => e.StudentId == studentId &&
                 e.Course.Code == courseCode,
            ct);
}

public async Task<EnrollmentResponseDto> AddAsync(
    Enrollment enrollment,
    CancellationToken ct)
{
    context.Enrollments.Add(enrollment);

    await context.SaveChangesAsync(ct);

    logger.LogInformation(
        "Created enrollment {EnrollmentId} for student {StudentId} in course {CourseId}",
        enrollment.Id,
        enrollment.StudentId,
        enrollment.CourseId);

    return (await GetByIdAsync(
        enrollment.CourseId,
        enrollment.Id,
        ct))!;
}

public async Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(
    int studentId,
    CancellationToken ct)
{
    return await context.Enrollments
        .AsNoTracking()
        .Include(e => e.Course)
        .Where(e => e.StudentId == studentId)
        .ToListAsync(ct);
}

}