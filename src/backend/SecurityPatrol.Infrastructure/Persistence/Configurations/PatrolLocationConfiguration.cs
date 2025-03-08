using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the PatrolLocation entity that implements 
    /// IEntityTypeConfiguration to define database mappings and relationships.
    /// </summary>
    public class PatrolLocationConfiguration : IEntityTypeConfiguration<PatrolLocation>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the PatrolLocation entity.
        /// </summary>
        /// <param name="builder">The builder used to configure the entity.</param>
        public void Configure(EntityTypeBuilder<PatrolLocation> builder)
        {
            // Configure the table name
            builder.ToTable("PatrolLocations");

            // Configure primary key
            builder.HasKey(p => p.Id);
            
            // Configure Id as identity
            builder.Property(p => p.Id)
                .UseIdentityColumn();

            // Configure properties
            builder.Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Configure GPS coordinates with appropriate precision
            builder.Property(p => p.Latitude)
                .HasPrecision(18, 15)
                .IsRequired();

            builder.Property(p => p.Longitude)
                .HasPrecision(18, 15)
                .IsRequired();

            builder.Property(p => p.LastUpdated)
                .IsRequired();

            builder.Property(p => p.RemoteId)
                .HasMaxLength(100)
                .IsRequired(false);

            // Configure the relationship with Checkpoint entities
            builder.HasMany(p => p.Checkpoints)
                .WithOne()
                .HasForeignKey("LocationId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure auditable entity properties
            builder.Property(p => p.CreatedBy)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(p => p.Created)
                .IsRequired();

            builder.Property(p => p.LastModifiedBy)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(p => p.LastModified)
                .IsRequired(false);
            
            // Add indexes for optimizing queries
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => new { p.Latitude, p.Longitude });
        }
    }
}