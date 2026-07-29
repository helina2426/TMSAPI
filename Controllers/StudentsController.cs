using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(TmsDbContext context) : ControllerBase
{

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudent(int id)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == id);

        if(student == null)
            return NotFound();

        return Ok(student);
    }

 // NEW - Get all students (query filter applies automatically)
    [HttpGet]
    public async Task<IActionResult> GetStudents()
    {
        var students = await context.Students
            .ToListAsync();

        return Ok(students);
    }

    // NEW - Admin endpoint (ignores query filter)
    [HttpGet("all")]
    public async Task<IActionResult> GetAllStudents()
    {
        var students = await context.Students
            .IgnoreQueryFilters()
            .ToListAsync();

        return Ok(students);
    }



    [HttpPut("{id}")]
public async Task<IActionResult> UpdateStudent(
    int id,
    StudentUpdateDto dto)
{
    var student = await context.Students
        .FirstOrDefaultAsync(s => s.Id == id);

    if (student == null)
        return NotFound();

    student.Name = dto.Name;
    student.GPA = dto.GPA;

    context.Entry(student)
        .Property(s => s.Version)
        .OriginalValue = dto.Version;

    try
    {
        await context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict("Concurrency conflict detected.");
    }

    return Ok(student);
}

  // NEW - Soft delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
            return NotFound();

        student.IsDeleted = true;

        await context.SaveChangesAsync();

        return Ok("Student soft deleted.");
    }
}


public record StudentUpdateDto(
    string Name,
    decimal GPA,
    uint Version
);
