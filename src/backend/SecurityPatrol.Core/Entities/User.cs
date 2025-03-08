using System;
using System.Collections.Generic;

namespace SecurityPatrol.Core.Entities
{
    /// <summary>
    /// Represents a security personnel user in the Security Patrol application.
    /// Inherits from AuditableEntity to track creation and modification information.
    /// </summary>
    public class User : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the phone number used for authentication and identification.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the user's last successful authentication.
        /// Used for session tracking and security monitoring.
        /// </summary>
        public DateTime LastAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// Inactive accounts cannot authenticate or use the system.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the collection of time records (clock in/out events) associated with this user.
        /// </summary>
        public ICollection<TimeRecord> TimeRecords { get; set; }

        /// <summary>
        /// Gets or sets the collection of location records tracked during active shifts.
        /// </summary>
        public ICollection<LocationRecord> LocationRecords { get; set; }

        /// <summary>
        /// Gets or sets the collection of photos captured by this user during patrols.
        /// </summary>
        public ICollection<Photo> Photos { get; set; }

        /// <summary>
        /// Gets or sets the collection of reports submitted by this user.
        /// </summary>
        public ICollection<Report> Reports { get; set; }

        /// <summary>
        /// Gets or sets the collection of checkpoint verifications completed by this user during patrols.
        /// </summary>
        public ICollection<CheckpointVerification> CheckpointVerifications { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        public User()
        {
            Id = Guid.NewGuid().ToString();
            IsActive = true;
            LastAuthenticated = DateTime.UtcNow;
            
            // Initialize collections to empty collections to prevent null reference exceptions
            TimeRecords = new List<TimeRecord>();
            LocationRecords = new List<LocationRecord>();
            Photos = new List<Photo>();
            Reports = new List<Report>();
            CheckpointVerifications = new List<CheckpointVerification>();
        }
    }
}