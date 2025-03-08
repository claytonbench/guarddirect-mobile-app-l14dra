using System;
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents an activity report in the Security Patrol application.
    /// Contains the report text, timestamp, location data, and synchronization status.
    /// </summary>
    public class ReportModel
    {
        /// <summary>
        /// The unique identifier for the report in the local database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the user who created the report.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The content of the activity report.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The date and time when the report was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The latitude coordinate where the report was created.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate where the report was created.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Indicates whether this report has been synchronized with the backend.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// The identifier of this report in the remote/backend system.
        /// Only populated after successful synchronization.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Default constructor for the ReportModel class.
        /// Initializes default values for a new report.
        /// </summary>
        public ReportModel()
        {
            Timestamp = DateTime.UtcNow;
            IsSynced = false;
        }

        /// <summary>
        /// Creates a ReportModel from a ReportEntity.
        /// </summary>
        /// <param name="entity">The ReportEntity to convert.</param>
        /// <returns>A new ReportModel instance with properties copied from the entity.</returns>
        public static ReportModel FromEntity(ReportEntity entity)
        {
            if (entity == null)
                return null;

            return new ReportModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Text = entity.Text,
                Timestamp = entity.Timestamp,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                IsSynced = entity.IsSynced,
                RemoteId = entity.RemoteId
            };
        }

        /// <summary>
        /// Converts this ReportModel to a ReportEntity.
        /// </summary>
        /// <returns>A new ReportEntity with properties copied from this model.</returns>
        public ReportEntity ToEntity()
        {
            return new ReportEntity
            {
                Id = this.Id,
                UserId = this.UserId,
                Text = this.Text,
                Timestamp = this.Timestamp,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                IsSynced = this.IsSynced,
                RemoteId = this.RemoteId
            };
        }

        /// <summary>
        /// Creates a deep copy of this ReportModel.
        /// </summary>
        /// <returns>A new ReportModel instance with the same property values.</returns>
        public ReportModel Clone()
        {
            return new ReportModel
            {
                Id = this.Id,
                UserId = this.UserId,
                Text = this.Text,
                Timestamp = this.Timestamp,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                IsSynced = this.IsSynced,
                RemoteId = this.RemoteId
            };
        }
    }
}