using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the Report entity that specifies table mappings,
    /// relationships, and constraints for database persistence.
    /// </summary>
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the Report entity
        /// </summary>
        /// <param name="builder">The entity type builder</param>
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            // Configure table name
            builder.ToTable("Reports");

            // Configure primary key with identity generation
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                .UseIdentityColumn();

            // Configure properties
            builder.Property(r => r.Text)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(r => r.Timestamp)
                .IsRequired();

            builder.Property(r => r.Latitude)
                .IsRequired()
                .HasPrecision(18, 15);

            builder.Property(r => r.Longitude)
                .IsRequired()
                .HasPrecision(18, 15);

            builder.Property(r => r.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(r => r.IsSynced)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(r => r.RemoteId)
                .HasMaxLength(100)
                .IsRequired(false);

            // Configure auditable properties
            builder.Property(r => r.CreatedBy)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(r => r.Created)
                .IsRequired();

            builder.Property(r => r.LastModifiedBy)
                .HasMaxLength(450)
                .IsRequired(false);

            builder.Property(r => r.LastModified)
                .IsRequired(false);

            // Configure indexes
            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_Reports_UserId");

            builder.HasIndex(r => r.IsSynced)
                .HasDatabaseName("IX_Reports_IsSynced");

            builder.HasIndex(r => r.Timestamp)
                .HasDatabaseName("IX_Reports_Timestamp");

            // Configure relationships
            builder.HasOne(r => r.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}