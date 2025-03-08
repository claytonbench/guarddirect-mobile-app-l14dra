using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the Checkpoint entity that defines database mappings,
    /// relationships, and constraints for Entity Framework Core.
    /// </summary>
    public class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the Checkpoint entity
        /// </summary>
        /// <param name="builder">The entity type builder to configure the entity</param>
        public void Configure(EntityTypeBuilder<Checkpoint> builder)
        {
            // Configure the table name
            builder.ToTable("Checkpoints");

            // Configure primary key with identity generation
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                .UseIdentityColumn();

            // Configure LocationId as required and create foreign key relationship
            builder.Property(c => c.LocationId)
                .IsRequired();

            // Configure Name property as required with maximum length
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Configure Latitude and Longitude as required with appropriate precision for GPS coordinates
            builder.Property(c => c.Latitude)
                .IsRequired()
                .HasPrecision(18, 15);

            builder.Property(c => c.Longitude)
                .IsRequired()
                .HasPrecision(18, 15);
            
            // Configure LastUpdated as required
            builder.Property(c => c.LastUpdated)
                .IsRequired();

            // Configure RemoteId as optional with maximum length
            builder.Property(c => c.RemoteId)
                .HasMaxLength(100)
                .IsRequired(false);

            // Configure the many-to-one relationship with PatrolLocation
            builder.HasOne(c => c.PatrolLocation)
                .WithMany(p => p.Checkpoints)
                .HasForeignKey(c => c.LocationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Configure the one-to-many relationship with CheckpointVerification
            builder.HasMany(c => c.Verifications)
                .WithOne(v => v.Checkpoint)
                .HasForeignKey(v => v.CheckpointId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure index on LocationId to optimize queries for checkpoints by location
            builder.HasIndex(c => c.LocationId)
                .HasDatabaseName("IX_Checkpoint_LocationId");

            // Configure auditable entity properties
            builder.Property(c => c.CreatedBy)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Created)
                .IsRequired();

            builder.Property(c => c.LastModifiedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(c => c.LastModified)
                .IsRequired(false);
        }
    }
}