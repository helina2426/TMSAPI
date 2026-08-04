using Microsoft.AspNetCore.Mvc;
using TmsApi.Services;
using TmsApi.Dtos;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{

[HttpGet]
public async Task<IActionResult> GetCourses(
    [FromQuery] PagedRequest request,
    CancellationToken ct)
{
    var result = await courseService.GetPagedAsync(request, ct);

    return Ok(result);
}

    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
public async Task<IActionResult> GetCourseById(
    int id,
    CancellationToken ct)
{
    var course = await courseService.GetByIdAsync(id, ct);

    return course is not null
        ? Ok(course)
        : NotFound();
}

    [HttpPost]
public async Task<IActionResult> CreateCourse(
    CreateCourseRequest request,
    CancellationToken ct)
{
    if (await courseService.CodeExistsAsync(request.Code, ct))
    {
        return Conflict(new ProblemDetails
        {
            Title = "Duplicate course code",
            Detail = $"A course with code '{request.Code}' already exists.",
            Status = StatusCodes.Status409Conflict
        });
    }

    var result = await courseService.CreateAsync(request, ct);

    return CreatedAtAction(
        nameof(GetCourseById),
        new { id = result.Id },
        result);
}



}