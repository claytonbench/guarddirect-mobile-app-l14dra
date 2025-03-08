using System;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Abstract base class that provides auditing capabilities for entity classes.
    /// Tracks creation and modification timestamps and the users who performed these actions.
    /// </summary>
    public abstract class AuditableEntity
    {
        /// <summary>
        /// Gets or sets the identifier of the user who created the entity.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified the entity.
        /// </summary>
        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// Null if the entity has not been modified since creation.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditableEntity"/> class.
        /// </summary>
        protected AuditableEntity()
        {
            // Default initialization - Created timestamp set to current UTC time
            Created = DateTime.UtcNow;
        }
    }
}