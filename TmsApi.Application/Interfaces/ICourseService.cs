using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(
        int id,
        CancellationToken ct);

    Task<CourseResponseDto> CreateAsync(
        CreateCourseRequest request,
        CancellationToken ct);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken ct);

    Task<PagedResponse<CourseResponseDto>> GetPagedAsync(
        PagedRequest request,
        CancellationToken ct);

    Task<Course?> GetByCodeAsync(
        string code,
        CancellationToken ct);

    Task<IReadOnlyList<CourseResponseDto>> GetAllAsync(
        CancellationToken ct);
}