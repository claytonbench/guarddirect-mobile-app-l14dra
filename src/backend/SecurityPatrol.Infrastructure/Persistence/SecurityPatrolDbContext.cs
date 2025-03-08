using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using System;
using System.Reflection;

namespace SecurityPatrol.Infrastructure.Persistence
{
    /// <summary>
    /// Entity Framework Core DbContext for the Security Patrol application that manages database access and entity configurations.
    /// </summary>
    public class SecurityPatrolDbContext : DbContext
    {
        private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

        /// <summary>
        /// Gets or sets the users in the database.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the time records (clock in/out events) in the database.
        /// </summary>
        public DbSet<TimeRecord> TimeRecords { get; set; }

        /// <summary>
        /// Gets or sets the location records tracked during active shifts in the database.
        /// </summary>
        public DbSet<LocationRecord> LocationRecords { get; set; }

        /// <summary>
        /// Gets or sets the patrol locations in the database.
        /// </summary>
        public DbSet<PatrolLocation> PatrolLocations { get; set; }

        /// <summary>
        /// Gets or sets the checkpoints within patrol locations in the database.
        /// </summary>
        public DbSet<Checkpoint> Checkpoints { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint verifications in the database.
        /// </summary>
        public DbSet<CheckpointVerification> CheckpointVerifications { get; set; }

        /// <summary>
        /// Gets or sets the photos captured during patrols in the database.
        /// </summary>
        public DbSet<Photo> Photos { get; set; }

        /// <summary>
        /// Gets or sets the activity reports in the database.
        /// </summary>
        public DbSet<Report> Reports { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPatrolDbContext"/> class with the specified options and auditing interceptor.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        /// <param name="auditableEntityInterceptor">The interceptor that automatically sets auditing information.</param>
        /// <exception cref="ArgumentNullException">Thrown if the auditableEntityInterceptor is null.</exception>
        public SecurityPatrolDbContext(
            DbContextOptions<SecurityPatrolDbContext> options,
            AuditableEntityInterceptor auditableEntityInterceptor)
            : base(options)
        {
            _auditableEntityInterceptor = auditableEntityInterceptor ?? 
                throw new ArgumentNullException(nameof(auditableEntityInterceptor));
        }

        /// <summary>
        /// Configures the entity model and relationships when building the model.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations found in the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Configure global query filters for soft-deleted entities
            // For example:
            // modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);
        }

        /// <summary>
        /// Configures the database context options when they are being created.
        /// </summary>
        /// <param name="optionsBuilder">A builder used to create or modify options for this context.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Add the auditable entity interceptor
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);

            // Configure additional options like command timeout, connection resiliency, etc.
            optionsBuilder.EnableSensitiveDataLogging(false);  // Set to true only in development
            
            // Example of setting command timeout:
            // optionsBuilder.CommandTimeout(60);
            
            // Example of configuring connection resiliency:
            // optionsBuilder.EnableRetryOnFailure(
            //     maxRetryCount: 5,
            //     maxRetryDelay: TimeSpan.FromSeconds(30),
            //     errorNumbersToAdd: null);
        }
    }
}