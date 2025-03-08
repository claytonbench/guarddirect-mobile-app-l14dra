using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Intercepts SaveChanges operations to automatically set auditing information 
    /// on entities that inherit from AuditableEntity.
    /// </summary>
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;

        /// <summary>
        /// Initializes a new instance of the AuditableEntityInterceptor class with the required services.
        /// </summary>
        /// <param name="currentUserService">Service to access current user information.</param>
        /// <param name="dateTime">Service to access current date and time.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public AuditableEntityInterceptor(ICurrentUserService currentUserService, IDateTime dateTime)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        }

        /// <summary>
        /// Intercepts synchronous SaveChanges operations to update auditing information.
        /// </summary>
        /// <param name="eventData">The event data containing the DbContext.</param>
        /// <param name="result">The interception result.</param>
        /// <returns>The interception result, typically unmodified.</returns>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, 
            InterceptionResult<int> result)
        {
            UpdateAuditableEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Intercepts asynchronous SaveChangesAsync operations to update auditing information.
        /// </summary>
        /// <param name="eventData">The event data containing the DbContext.</param>
        /// <param name="result">The interception result.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The interception result, typically unmodified.</returns>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, 
            InterceptionResult<int> result, 
            CancellationToken cancellationToken = default)
        {
            UpdateAuditableEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Updates the auditing information for all AuditableEntity instances 
        /// being tracked by the context.
        /// </summary>
        /// <param name="context">The database context.</param>
        private void UpdateAuditableEntities(DbContext context)
        {
            if (context == null)
            {
                return;
            }

            // Get all entity entries that are of type AuditableEntity
            var auditableEntities = context.ChangeTracker.Entries<AuditableEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            if (!auditableEntities.Any())
            {
                return;
            }

            // Get current user and timestamp information
            string userId = _currentUserService.GetUserId();
            DateTime utcNow = _dateTime.Now;

            foreach (var entry in auditableEntities)
            {
                if (entry.State == EntityState.Added)
                {
                    // For new entities, set creation information
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.Created = utcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    // For modified entities, set modification information
                    entry.Entity.LastModifiedBy = userId;
                    entry.Entity.LastModified = utcNow;
                }
            }
        }
    }
}