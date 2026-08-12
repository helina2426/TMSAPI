using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Grade)
            .HasColumnType("numeric(3,2)");

        builder.Property(e => e.EnrolledAt)
            .IsRequired();

        // Prevent deleting a student while enrollment records exist.
     builder.HasOne(e => e.Student)
    .WithMany(s => s.Enrollments)
    .HasForeignKey(e => e.StudentId)
    .OnDelete(DeleteBehavior.Restrict);

// Prevent deleting a course while enrollment records exist.
    builder.HasOne(e => e.Course)
    .WithMany(c => c.Enrollments)
    .HasForeignKey(e => e.CourseId)
    .OnDelete(DeleteBehavior.Restrict);
    }
}