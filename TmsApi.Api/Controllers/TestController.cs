using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{

    // Exercise 2 - Step 3
    // Demonstrates LINQ deferred execution
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building query object (no database contact)...");

        var query = context.Students
            .Where(s => s.GPA >= 3.0m);


        Console.WriteLine(">>> STEP 2: Adding sorting...");

        var orderedQuery = query
            .OrderBy(s => s.Name);


        Console.WriteLine(">>> STEP 3: Calling ToList (execution happens here)...");

        var results = orderedQuery.ToList();


        Console.WriteLine(">>> STEP 4: Finished\n");


        return Ok(results);
    }



    // Exercise 2 - Step 4
    // Helper method that EF Core cannot translate into SQL
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }



    // Exercise 2 - Step 4
    // Demonstrates SQL translation failure
    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...\n");


        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA))
                .ToList();


            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $">>> EXCEPTION CAUGHT: {ex.Message}\n"
            );


            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }

        // Exercise 2 - Step 5 - Query 1
    // Count active students with GPA >= 3.0

    [HttpGet("active-count")]
    public async Task<IActionResult> ActiveStudentCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(new
        {
            Count = count
        });
    }

    // Exercise 2 - Step 5 - Query 2
    // Which courses have the most enrollments?

    [HttpGet("course-enrollments")]
    public async Task<IActionResult> CourseEnrollments()
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        return Ok(list);
    }

// Exercise 2 - Step 5 - Query 3
// Average GPA per course

[HttpGet("average-gpa")]
public async Task<IActionResult> AverageGpaPerCourse()
{
    var list = await context.Enrollments
        .GroupBy(e => e.Course.Title)
        .Select(g => new
        {
            Course = g.Key,
            AverageGPA = g.Average(e => e.Student.GPA)
        })
        .ToListAsync();

    return Ok(list);
}

// Exercise 2 - Step 5 - Query 4A
// Students with zero enrollments (NOT EXISTS)

[HttpGet("students-no-enrollments")]
public async Task<IActionResult> StudentsWithNoEnrollments()
{
    var list = await context.Students
        .Where(s => !s.Enrollments.Any())
        .Select(s => s.Name)
        .ToListAsync();

    return Ok(list);
}

// Exercise 2 - Step 5 - Query 4B
// Students with zero enrollments using LeftJoin

[HttpGet("students-no-enrollments-leftjoin")]
public async Task<IActionResult> StudentsWithNoEnrollmentsLeftJoin()
{
    var list = await context.Students
        .LeftJoin(
            context.Enrollments,
            s => s.Id,
            e => e.StudentId,
            (s, e) => new { s, e })
        .Where(x => x.e == null)
        .Select(x => x.s.Name)
        .ToListAsync();

    return Ok(list);
}

// Exercise 3 - Step 1
// Paginated student list
// OrderBy must happen before Skip/Take

[HttpGet("students-page")]
public async Task<IActionResult> StudentsPage(
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    var students = await context.Students
        .OrderBy(s => s.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return Ok(students);
}

// Exercise 3 - Step 2
// Top 5 courses by enrollment count

[HttpGet("top-courses")]
public async Task<IActionResult> TopCourses()
{
    var courses = await context.Enrollments
        .GroupBy(e => e.Course.Title)
        .Select(g => new
        {
            Course = g.Key,
            EnrollmentCount = g.Count()
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .Take(5)
        .ToListAsync();

    return Ok(courses);
}

// Exercise 7 - Part A
// Demonstrates the N+1 query problem

[HttpGet("nplus1")]
public async Task<IActionResult> NPlusOne(CancellationToken cancellationToken)
{
    var students = await context.Students
        .AsNoTracking()
        .ToListAsync(cancellationToken);

    foreach (var student in students)
    {
        var count = await context.Enrollments
            .AsNoTracking()
            .CountAsync(
                e => e.StudentId == student.Id,
                cancellationToken);

        Console.WriteLine($"{student.Name}: {count} enrollments");
    }

    return Ok("Check the console SQL log.");
}

[HttpGet("include")]
public async Task<IActionResult> IncludeExample(
    CancellationToken cancellationToken)
{
    var students = await context.Students
        .Include(s => s.Enrollments)
        .AsNoTracking()
        .ToListAsync(cancellationToken);

    foreach (var student in students)
    {
        Console.WriteLine(
            $"{student.Name}: {student.Enrollments.Count} enrollments");
    }

    return Ok("Check the console SQL log.");
}

// Exercise 9 - Bulk archive
[HttpPost("archive-enrollments")]
public async Task<IActionResult> ArchiveEnrollments(
    CancellationToken cancellationToken)
{
    var cutoff = DateTime.UtcNow.AddDays(1);

    var affectedRows = await context.Enrollments
        .Where(e => e.EnrolledAt < cutoff)
        .ExecuteUpdateAsync(
            s => s.SetProperty(
                e => e.IsArchived,
                true),
            cancellationToken);

    return Ok(new
    {
        Archived = affectedRows
    });
}

}