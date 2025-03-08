using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the TimeRecord entity that implements IEntityTypeConfiguration<TimeRecord>
    /// to define database mappings and relationships.
    /// </summary>
    public class TimeRecordConfiguration : IEntityTypeConfiguration<TimeRecord>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the TimeRecord entity
        /// </summary>
        /// <param name="builder">The entity type builder used to configure the entity</param>
        public void Configure(EntityTypeBuilder<TimeRecord> builder)
        {
            // Configure the table name
            builder.ToTable("TimeRecords");

            // Configure primary key with identity generation
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id)
                .UseIdentityColumn();

            // Configure Type as required with maximum length of 20 characters
            builder.Property(t => t.Type)
                .IsRequired()
                .HasMaxLength(20);

            // Configure Timestamp as required
            builder.Property(t => t.Timestamp)
                .IsRequired();

            // Configure Latitude and Longitude as required with precision and scale appropriate for GPS coordinates
            builder.Property(t => t.Latitude)
                .IsRequired()
                .HasPrecision(18, 15);

            builder.Property(t => t.Longitude)
                .IsRequired()
                .HasPrecision(18, 15);

            // Configure UserId as required with maximum length of 450 characters
            builder.Property(t => t.UserId)
                .IsRequired()
                .HasMaxLength(450);

            // Configure IsSynced as required with a default value of false
            builder.Property(t => t.IsSynced)
                .IsRequired()
                .HasDefaultValue(false);

            // Configure RemoteId as optional with maximum length of 100 characters
            builder.Property(t => t.RemoteId)
                .HasMaxLength(100);

            // Create an index on UserId to optimize queries by user
            builder.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_TimeRecord_UserId");

            // Create an index on IsSynced to optimize sync queries
            builder.HasIndex(t => t.IsSynced)
                .HasDatabaseName("IX_TimeRecord_IsSynced");

            // Create a composite index on UserId and Timestamp to optimize time record history queries
            builder.HasIndex(t => new { t.UserId, t.Timestamp })
                .HasDatabaseName("IX_TimeRecord_UserId_Timestamp");

            // Configure the one-to-many relationship with User (many time records belong to one user)
            builder.HasOne(t => t.User)
                .WithMany(u => u.TimeRecords)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure auditable entity properties (inherited from AuditableEntity)
            builder.Property(t => t.CreatedBy)
                .HasMaxLength(450);

            builder.Property(t => t.Created)
                .IsRequired();

            builder.Property(t => t.LastModifiedBy)
                .HasMaxLength(450);
        }
    }
}