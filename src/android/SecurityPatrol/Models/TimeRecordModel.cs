using System;  // Version 8.0+
using CommunityToolkit.Mvvm.ComponentModel;  // Version Latest
using SecurityPatrol.Database.Entities;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Represents a clock in/out event in the Security Patrol application.
    /// This model is used for time tracking operations and UI display.
    /// </summary>
    [ObservableObject]
    public partial class TimeRecordModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for this time record.
        /// </summary>
        [ObservableProperty]
        private int id;

        /// <summary>
        /// Gets or sets the user identifier associated with this time record.
        /// </summary>
        [ObservableProperty]
        private string userId;

        /// <summary>
        /// Gets or sets the type of time record (e.g., "ClockIn", "ClockOut").
        /// </summary>
        [ObservableProperty]
        private string type;

        /// <summary>
        /// Gets or sets the timestamp when the clock event occurred.
        /// </summary>
        [ObservableProperty]
        private DateTime timestamp;

        /// <summary>
        /// Gets or sets the latitude coordinate where the clock event occurred.
        /// </summary>
        [ObservableProperty]
        private double latitude;

        /// <summary>
        /// Gets or sets the longitude coordinate where the clock event occurred.
        /// </summary>
        [ObservableProperty]
        private double longitude;

        /// <summary>
        /// Gets or sets a value indicating whether this record has been synchronized with the backend.
        /// </summary>
        [ObservableProperty]
        private bool isSynced;

        /// <summary>
        /// Gets or sets the remote identifier assigned by the backend API after synchronization.
        /// </summary>
        [ObservableProperty]
        private string remoteId;

        /// <summary>
        /// Gets the formatted time string for display in the UI.
        /// </summary>
        public string FormattedTime => GetFormattedTime();

        /// <summary>
        /// Gets the formatted date string for display in the UI.
        /// </summary>
        public string FormattedDate => GetFormattedDate();

        /// <summary>
        /// Gets a user-friendly display string for the record type.
        /// </summary>
        public string TypeDisplay => GetTypeDisplay();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRecordModel"/> class.
        /// </summary>
        public TimeRecordModel()
        {
            Id = 0;
            UserId = string.Empty;
            Type = string.Empty;
            Timestamp = DateTime.Now;
            Latitude = 0.0;
            Longitude = 0.0;
            IsSynced = false;
            RemoteId = string.Empty;
        }

        /// <summary>
        /// Creates a TimeRecordModel from a TimeRecordEntity.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <returns>A new TimeRecordModel populated with data from the entity.</returns>
        public static TimeRecordModel FromEntity(TimeRecordEntity entity)
        {
            return new TimeRecordModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Type = entity.Type,
                Timestamp = entity.Timestamp,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                IsSynced = entity.IsSynced,
                RemoteId = entity.RemoteId
            };
        }

        /// <summary>
        /// Converts this TimeRecordModel to a TimeRecordEntity.
        /// </summary>
        /// <returns>A new TimeRecordEntity populated with data from this model.</returns>
        public TimeRecordEntity ToEntity()
        {
            return new TimeRecordEntity
            {
                Id = Id,
                UserId = UserId,
                Type = Type,
                Timestamp = Timestamp,
                Latitude = Latitude,
                Longitude = Longitude,
                IsSynced = IsSynced,
                RemoteId = RemoteId
            };
        }

        /// <summary>
        /// Determines if this record represents a clock-in event.
        /// </summary>
        /// <returns>True if this is a clock-in event, false otherwise.</returns>
        public bool IsClockIn()
        {
            return Type == "ClockIn";
        }

        /// <summary>
        /// Determines if this record represents a clock-out event.
        /// </summary>
        /// <returns>True if this is a clock-out event, false otherwise.</returns>
        public bool IsClockOut()
        {
            return Type == "ClockOut";
        }

        /// <summary>
        /// Gets the formatted time string for display in the UI.
        /// </summary>
        /// <returns>The formatted time string (e.g., '3:45 PM').</returns>
        private string GetFormattedTime()
        {
            return Timestamp.ToString("h:mm tt");
        }

        /// <summary>
        /// Gets the formatted date string for display in the UI.
        /// </summary>
        /// <returns>The formatted date string (e.g., 'Jan 15, 2023').</returns>
        private string GetFormattedDate()
        {
            return Timestamp.ToString("MMM d, yyyy");
        }

        /// <summary>
        /// Gets a user-friendly display string for the record type.
        /// </summary>
        /// <returns>The display string ('Clock In' or 'Clock Out').</returns>
        private string GetTypeDisplay()
        {
            if (IsClockIn())
                return "Clock In";
            if (IsClockOut())
                return "Clock Out";
            return Type;
        }

        /// <summary>
        /// Creates a new TimeRecordModel for a clock-in event.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <returns>A new TimeRecordModel configured for clock-in.</returns>
        public static TimeRecordModel CreateClockIn(string userId, double latitude, double longitude)
        {
            return new TimeRecordModel
            {
                UserId = userId,
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                IsSynced = false
            };
        }

        /// <summary>
        /// Creates a new TimeRecordModel for a clock-out event.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <returns>A new TimeRecordModel configured for clock-out.</returns>
        public static TimeRecordModel CreateClockOut(string userId, double latitude, double longitude)
        {
            return new TimeRecordModel
            {
                UserId = userId,
                Type = "ClockOut",
                Timestamp = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                IsSynced = false
            };
        }
    }
}