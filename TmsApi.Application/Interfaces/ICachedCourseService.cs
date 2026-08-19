using TmsApi.Application.Dtos;

namespace TmsApi.Application.Interfaces;

public interface ICachedCourseService
{
    Task<IReadOnlyList<CourseResponseDto>> GetAllCoursesAsync(
        CancellationToken ct);

    Task InvalidateCourseCacheAsync(
        CancellationToken ct);
} 