using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.GPA)
            .HasColumnType("numeric(3,2)");

        builder.Property(s => s.IsActive)
            .IsRequired();
        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique();    
        builder.Property<DateTime>("LastUpdated")
            .HasColumnType("timestamp without time zone")        
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(s => s.Version)
            .IsRowVersion(); 

         builder.HasQueryFilter(s => !s.IsDeleted);          
    }
}