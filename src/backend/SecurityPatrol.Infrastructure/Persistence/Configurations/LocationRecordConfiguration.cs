using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the LocationRecord entity that implements IEntityTypeConfiguration
    /// to define database mappings and relationships.
    /// </summary>
    public class LocationRecordConfiguration : IEntityTypeConfiguration<LocationRecord>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the LocationRecord entity.
        /// </summary>
        /// <param name="builder">The entity type builder to be used for configuration.</param>
        public void Configure(EntityTypeBuilder<LocationRecord> builder)
        {
            // Configure table name
            builder.ToTable("LocationRecords");

            // Configure primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd(); // Auto-increment ID

            // Configure properties
            builder.Property(e => e.Latitude)
                .IsRequired()
                .HasPrecision(18, 15); // High precision for GPS coordinates

            builder.Property(e => e.Longitude)
                .IsRequired()
                .HasPrecision(18, 15); // High precision for GPS coordinates

            builder.Property(e => e.Accuracy)
                .IsRequired()
                .HasPrecision(10, 2); // Two decimal places for accuracy in meters

            builder.Property(e => e.Timestamp)
                .IsRequired();

            builder.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450); // Max length matching ASP.NET Core Identity user ID

            builder.Property(e => e.IsSynced)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.RemoteId)
                .HasMaxLength(100);

            // Configure indexes for performance optimization
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_LocationRecord_UserId");

            builder.HasIndex(e => e.IsSynced)
                .HasDatabaseName("IX_LocationRecord_IsSynced");

            builder.HasIndex(e => new { e.UserId, e.Timestamp })
                .HasDatabaseName("IX_LocationRecord_UserId_Timestamp");

            // Configure relationships
            builder.HasOne(e => e.User)
                .WithMany(e => e.LocationRecords)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Delete location records when user is deleted
        }
    }
}