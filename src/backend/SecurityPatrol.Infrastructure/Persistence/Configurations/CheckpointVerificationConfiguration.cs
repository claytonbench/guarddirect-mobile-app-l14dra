using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuration class for the CheckpointVerification entity that maps the entity to the database table
    /// and configures relationships, constraints, and indexes.
    /// </summary>
    public class CheckpointVerificationConfiguration : IEntityTypeConfiguration<CheckpointVerification>
    {
        /// <summary>
        /// Configures the Entity Framework Core mapping for the CheckpointVerification entity.
        /// </summary>
        /// <param name="builder">The entity type builder used to configure the entity.</param>
        public void Configure(EntityTypeBuilder<CheckpointVerification> builder)
        {
            // Configure table name
            builder.ToTable("CheckpointVerifications");

            // Configure primary key
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id)
                .UseIdentityColumn()
                .IsRequired();

            // Configure properties
            builder.Property(v => v.UserId)
                .IsRequired();

            builder.Property(v => v.CheckpointId)
                .IsRequired();

            builder.Property(v => v.Timestamp)
                .IsRequired();

            builder.Property(v => v.Latitude)
                .IsRequired()
                .HasPrecision(18, 15);

            builder.Property(v => v.Longitude)
                .IsRequired()
                .HasPrecision(18, 15);

            builder.Property(v => v.IsSynced)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(v => v.RemoteId)
                .HasMaxLength(100)
                .IsRequired(false);

            // Configure relationships
            builder.HasOne(v => v.User)
                .WithMany(u => u.CheckpointVerifications)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.Checkpoint)
                .WithMany(c => c.Verifications)
                .HasForeignKey(v => v.CheckpointId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes
            builder.HasIndex(v => v.UserId)
                .HasDatabaseName("IX_CheckpointVerification_UserId");

            builder.HasIndex(v => v.CheckpointId)
                .HasDatabaseName("IX_CheckpointVerification_CheckpointId");

            builder.HasIndex(v => new { v.UserId, v.Timestamp })
                .HasDatabaseName("IX_CheckpointVerification_UserId_Timestamp");

            builder.HasIndex(v => v.IsSynced)
                .HasDatabaseName("IX_CheckpointVerification_IsSynced");

            // Configure auditable entity properties
            builder.Property(v => v.CreatedBy)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(v => v.Created)
                .IsRequired();

            builder.Property(v => v.LastModifiedBy)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(v => v.LastModified)
                .IsRequired(false);
        }
    }
}