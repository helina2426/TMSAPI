using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseService service,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{
    public async Task<IReadOnlyList<CourseResponseDto>> GetAllCoursesAsync(
        CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var courses = await cache.GetOrCreateAsync(
            key,
            service,
            async (state, token) =>
            {
                dbHit = true;

                logger.LogInformation(
                    "Cache MISS for {Key} fetching from DB",
                    key);

                return await state.GetAllAsync(token);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }

        return courses;
    }

    public async Task InvalidateCourseCacheAsync(
        CancellationToken ct)
    {
        logger.LogInformation(
            "Invalidating cache tag {Tag}",
            CacheKeys.CoursesTag);

        await cache.RemoveByTagAsync(
            CacheKeys.CoursesTag,
            ct);
    }
}