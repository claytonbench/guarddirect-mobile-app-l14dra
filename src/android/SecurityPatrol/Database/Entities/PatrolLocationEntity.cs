using System;
using SQLite; // SQLite-net-pcl 1.8+
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a patrol location in the local SQLite database.
    /// Contains properties for identification, name, geographic coordinates, and synchronization with the backend.
    /// </summary>
    [Table(DatabaseConstants.TablePatrolLocation)]
    public class PatrolLocationEntity
    {
        /// <summary>
        /// The primary key identifier for the patrol location
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// The name of the patrol location
        /// </summary>
        [Indexed]
        public string Name { get; set; }

        /// <summary>
        /// The latitude coordinate of the patrol location
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate of the patrol location
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// The timestamp when this patrol location was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The remote identifier from the backend API for this location
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// Default constructor for the PatrolLocationEntity class
        /// </summary>
        public PatrolLocationEntity()
        {
            // Initialize properties with default values
            Name = string.Empty;
            Latitude = 0;
            Longitude = 0;
            LastUpdated = DateTime.UtcNow;
            RemoteId = string.Empty;
        }

        /// <summary>
        /// Converts this entity to a LocationModel for use in the application's business logic
        /// </summary>
        /// <returns>A new LocationModel populated with data from this entity</returns>
        public LocationModel ToLocationModel()
        {
            return new LocationModel
            {
                Id = Id,
                Name = Name,
                Latitude = Latitude,
                Longitude = Longitude,
                RemoteId = RemoteId
            };
        }

        /// <summary>
        /// Creates a PatrolLocationEntity from a LocationModel
        /// </summary>
        /// <param name="model">The LocationModel to convert</param>
        /// <returns>A new PatrolLocationEntity populated with data from the model</returns>
        public static PatrolLocationEntity FromLocationModel(LocationModel model)
        {
            return new PatrolLocationEntity
            {
                Id = model.Id,
                Name = model.Name,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                RemoteId = model.RemoteId,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}