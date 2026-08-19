using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace TmsApi.Infrastructure.Services;


public class CourseService(
    TmsDbContext context,
    ILogger<CourseService> logger)
    : ICourseService
{
    public async Task<CourseResponseDto?> GetByIdAsync(
        int id,
        CancellationToken ct)
    {
        return await context.Courses
    .AsNoTracking()
    .Where(c => c.Id == id)
    .Select(c => new CourseResponseDto(
        c.Id,
        c.Code,
        c.Title,
        c.MaxCapacity,
        c.Enrollments.Count))
    .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResponse<CourseResponseDto>> GetPagedAsync(
    PagedRequest request,
    CancellationToken ct)
{
    var query = context.Courses
        .AsNoTracking();

    // Search
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        var search = request.Search.Trim();

        query = query.Where(c =>
            c.Code.Contains(search) ||
            c.Title.Contains(search));
    }

    // Ordering
    query = request.OrderBy.ToLowerInvariant() switch
    {
        "code" => request.Descending
            ? query.OrderByDescending(c => c.Code)
            : query.OrderBy(c => c.Code),

        "title" => request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title),

        "maxcapacity" => request.Descending
            ? query.OrderByDescending(c => c.MaxCapacity)
            : query.OrderBy(c => c.MaxCapacity),

        _ => request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title)
    };

    // Total count before pagination
    var totalCount = await query.CountAsync(ct);

    // Pagination
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);

    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}

    public async Task<bool> CodeExistsAsync(
    string code,
    CancellationToken ct)
{
    return await context.Courses
        .AnyAsync(c => c.Code == code, ct);
}

    public async Task<CourseResponseDto> CreateAsync(
    CreateCourseRequest request,
    CancellationToken ct)
{
    var course = new Course
    {
        Code = request.Code,
        Title = request.Title,
        MaxCapacity = request.MaxCapacity
    };

    context.Courses.Add(course);

    await context.SaveChangesAsync(ct);

    logger.LogInformation(
        "Created course {CourseId} ({Code})",
        course.Id,
        course.Code);

    return (await GetByIdAsync(course.Id, ct))!;
}

public async Task<Course?> GetByCodeAsync(
    string code,
    CancellationToken ct)
{
    return await context.Courses
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Code == code, ct);
}

public async Task<IReadOnlyList<CourseResponseDto>> GetAllAsync(
    CancellationToken ct)
{
    return await context.Courses
        .AsNoTracking()
        .OrderBy(c => c.Title)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);
}

}