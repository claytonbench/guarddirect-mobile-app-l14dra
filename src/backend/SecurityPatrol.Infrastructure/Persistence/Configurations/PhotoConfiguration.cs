using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the Photo entity that implements IEntityTypeConfiguration<Photo> to define
    /// database mappings and relationships.
    /// </summary>
    public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the Photo entity.
        /// </summary>
        /// <param name="builder">The entity type builder used to configure the entity.</param>
        public void Configure(EntityTypeBuilder<Photo> builder)
        {
            // Configure the table name
            builder.ToTable("Photos");

            // Configure primary key
            builder.HasKey(p => p.Id);
            
            // Configure auto-increment for Id
            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            // Configure required properties
            builder.Property(p => p.Timestamp)
                .IsRequired();

            // Configure GPS coordinates with appropriate precision for latitude/longitude
            builder.Property(p => p.Latitude)
                .IsRequired()
                .HasPrecision(18, 15);  // Precision and scale suitable for GPS coordinates

            builder.Property(p => p.Longitude)
                .IsRequired()
                .HasPrecision(18, 15);  // Precision and scale suitable for GPS coordinates

            // Configure FilePath with appropriate length constraint
            builder.Property(p => p.FilePath)
                .IsRequired()
                .HasMaxLength(500);  // Maximum length for file paths

            // Configure foreign key
            builder.Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(450);  // Maximum length matching typical identity user IDs

            // Configure synchronization properties
            builder.Property<bool>("IsSynced")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property<string>("RemoteId")
                .HasMaxLength(100);

            builder.Property<int>("SyncProgress")
                .HasDefaultValue(0);

            // Configure indexes for optimizing queries
            builder.HasIndex(p => p.UserId);  // Index for filtering by user
            builder.HasIndex("IsSynced");     // Index for filtering by sync status
            builder.HasIndex(p => p.Timestamp);  // Index for filtering by time

            // Configure the relationship with User
            builder.HasOne(p => p.User)
                .WithMany(u => u.Photos)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // If user is deleted, delete all their photos

            // Configure auditable entity properties - these come from the base class
            builder.Property(p => p.Created)
                .IsRequired();

            builder.Property(p => p.CreatedBy)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(p => p.LastModified)
                .IsRequired(false);

            builder.Property(p => p.LastModifiedBy)
                .IsRequired(false)
                .HasMaxLength(450);
        }
    }
}