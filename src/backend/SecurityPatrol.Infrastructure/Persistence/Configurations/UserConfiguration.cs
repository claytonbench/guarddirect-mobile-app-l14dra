using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the User entity that implements IEntityTypeConfiguration<User>
    /// to define database mappings and relationships.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the User entity.
        /// </summary>
        /// <param name="builder">The entity type builder used to configure the entity.</param>
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Configure the table name as 'Users'
            builder.ToTable("Users");

            // Configure Id as the primary key with maximum length of 450 characters
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasMaxLength(450)
                .IsRequired();

            // Configure PhoneNumber as required with maximum length of 20 characters
            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            // Configure LastAuthenticated as required
            builder.Property(u => u.LastAuthenticated)
                .IsRequired();

            // Configure IsActive as required with a default value of true
            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Create a unique index on PhoneNumber to ensure each phone number can only be registered once
            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique();

            // Configure one-to-many relationship with TimeRecords (one user has many time records)
            builder.HasMany(u => u.TimeRecords)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship with LocationRecords (one user has many location records)
            builder.HasMany(u => u.LocationRecords)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship with Photos (one user has many photos)
            builder.HasMany(u => u.Photos)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship with Reports (one user has many reports)
            builder.HasMany(u => u.Reports)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship with CheckpointVerifications (one user has many checkpoint verifications)
            builder.HasMany(u => u.CheckpointVerifications)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}