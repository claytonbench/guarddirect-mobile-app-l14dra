using Microsoft.EntityFrameworkCore; // v8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System; // v8.0.0
using System.Collections.Generic; // v8.0.0
using System.Linq; // v8.0.0
using System.Threading.Tasks; // v8.0.0

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of the IPatrolLocationRepository interface that provides data access operations 
    /// for PatrolLocation entities using Entity Framework Core.
    /// </summary>
    public class PatrolLocationRepository : IPatrolLocationRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the PatrolLocationRepository class with the specified database context.
        /// </summary>
        /// <param name="context">The database context to use for data access operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if the context is null.</exception>
        public PatrolLocationRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a patrol location by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the patrol location to retrieve.</param>
        /// <returns>The patrol location with the specified ID, or null if not found.</returns>
        public async Task<PatrolLocation> GetByIdAsync(int id)
        {
            return await _context.PatrolLocations.FindAsync(id);
        }

        /// <summary>
        /// Retrieves all patrol locations in the system.
        /// </summary>
        /// <returns>A collection of all patrol locations.</returns>
        public async Task<IEnumerable<PatrolLocation>> GetAllAsync()
        {
            return await _context.PatrolLocations.ToListAsync();
        }

        /// <summary>
        /// Retrieves a patrol location by ID including its associated checkpoints.
        /// </summary>
        /// <param name="id">The ID of the patrol location to retrieve.</param>
        /// <returns>The patrol location with checkpoints included, or null if not found.</returns>
        public async Task<PatrolLocation> GetWithCheckpointsAsync(int id)
        {
            return await _context.PatrolLocations
                .Include(p => p.Checkpoints)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Retrieves all patrol locations including their associated checkpoints.
        /// </summary>
        /// <returns>A collection of all patrol locations with their checkpoints.</returns>
        public async Task<IEnumerable<PatrolLocation>> GetAllWithCheckpointsAsync()
        {
            return await _context.PatrolLocations
                .Include(p => p.Checkpoints)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new patrol location to the system.
        /// </summary>
        /// <param name="location">The patrol location to add.</param>
        /// <returns>A result containing the ID of the newly created patrol location if successful.</returns>
        public async Task<Result<int>> AddAsync(PatrolLocation location)
        {
            try
            {
                if (location == null)
                {
                    return Result.Failure<int>("Patrol location cannot be null.");
                }

                if (string.IsNullOrWhiteSpace(location.Name))
                {
                    return Result.Failure<int>("Patrol location name is required.");
                }

                // Validate latitude and longitude
                if (location.Latitude < -90 || location.Latitude > 90)
                {
                    return Result.Failure<int>("Latitude must be between -90 and 90 degrees.");
                }

                if (location.Longitude < -180 || location.Longitude > 180)
                {
                    return Result.Failure<int>("Longitude must be between -180 and 180 degrees.");
                }

                // Ensure LastUpdated is set to current UTC time
                location.LastUpdated = DateTime.UtcNow;

                await _context.PatrolLocations.AddAsync(location);
                await _context.SaveChangesAsync();

                return Result.Success(location.Id, "Patrol location created successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure<int>($"Failed to create patrol location: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing patrol location in the system.
        /// </summary>
        /// <param name="location">The patrol location to update.</param>
        /// <returns>A result indicating success or failure of the update operation.</returns>
        public async Task<Result> UpdateAsync(PatrolLocation location)
        {
            try
            {
                if (location == null)
                {
                    return Result.Failure("Patrol location cannot be null.");
                }

                if (location.Id <= 0)
                {
                    return Result.Failure("Invalid patrol location ID.");
                }

                if (string.IsNullOrWhiteSpace(location.Name))
                {
                    return Result.Failure("Patrol location name is required.");
                }

                // Validate latitude and longitude
                if (location.Latitude < -90 || location.Latitude > 90)
                {
                    return Result.Failure("Latitude must be between -90 and 90 degrees.");
                }

                if (location.Longitude < -180 || location.Longitude > 180)
                {
                    return Result.Failure("Longitude must be between -180 and 180 degrees.");
                }

                // Check if the location exists
                bool exists = await ExistsAsync(location.Id);
                if (!exists)
                {
                    return Result.Failure($"Patrol location with ID {location.Id} not found.");
                }

                // Ensure LastUpdated is set to current UTC time
                location.LastUpdated = DateTime.UtcNow;

                _context.PatrolLocations.Update(location);
                await _context.SaveChangesAsync();

                return Result.Success("Patrol location updated successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to update patrol location: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a patrol location from the system.
        /// </summary>
        /// <param name="id">The ID of the patrol location to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var location = await GetByIdAsync(id);
                if (location == null)
                {
                    return Result.Failure($"Patrol location with ID {id} not found.");
                }

                // Check if the location has associated checkpoints
                bool hasCheckpoints = await _context.Checkpoints.AnyAsync(c => c.LocationId == id);
                if (hasCheckpoints)
                {
                    return Result.Failure("Cannot delete patrol location with associated checkpoints. Please delete the checkpoints first.");
                }

                _context.PatrolLocations.Remove(location);
                await _context.SaveChangesAsync();

                return Result.Success("Patrol location deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete patrol location: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves patrol locations within a specified distance of a given location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point.</param>
        /// <param name="longitude">The longitude coordinate of the center point.</param>
        /// <param name="radiusInMeters">The radius in meters within which to find patrol locations.</param>
        /// <returns>A collection of patrol locations within the specified radius.</returns>
        public async Task<IEnumerable<PatrolLocation>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusInMeters)
        {
            // Validate input parameters
            if (latitude < -90 || latitude > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees.");
            }

            if (longitude < -180 || longitude > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees.");
            }

            if (radiusInMeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusInMeters), "Radius must be a positive value.");
            }

            // First, perform a coarse filtering to reduce the number of distance calculations
            // Convert radius from meters to approximate degrees for latitude and longitude
            // 1 degree of latitude is approximately 111,000 meters (varies slightly with latitude)
            // 1 degree of longitude varies with latitude (cos(latitude) * 111,000 meters)
            
            double latDegrees = radiusInMeters / 111000;
            double lonDegrees = radiusInMeters / (111000 * Math.Cos(latitude * Math.PI / 180));
            
            // Get an approximate bounding box
            double minLat = latitude - latDegrees;
            double maxLat = latitude + latDegrees;
            double minLon = longitude - lonDegrees;
            double maxLon = longitude + lonDegrees;
            
            // First, filter by the bounding box (more efficient than calculating exact distances for all locations)
            var potentialLocations = await _context.PatrolLocations
                .Where(p => p.Latitude >= minLat && p.Latitude <= maxLat && 
                           p.Longitude >= minLon && p.Longitude <= maxLon)
                .ToListAsync();
            
            // Then, filter the results to include only locations within the exact radius using the Haversine formula
            return potentialLocations
                .Where(p => CalculateDistance(latitude, longitude, p.Latitude, p.Longitude) <= radiusInMeters)
                .ToList();
        }

        /// <summary>
        /// Checks if a patrol location with the specified ID exists in the system.
        /// </summary>
        /// <param name="id">The ID of the patrol location to check.</param>
        /// <returns>True if the patrol location exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.PatrolLocations.AnyAsync(p => p.Id == id);
        }

        /// <summary>
        /// Calculates the distance between two geographic coordinates using the Haversine formula.
        /// </summary>
        /// <param name="lat1">The latitude of the first point in degrees.</param>
        /// <param name="lon1">The longitude of the first point in degrees.</param>
        /// <param name="lat2">The latitude of the second point in degrees.</param>
        /// <param name="lon2">The longitude of the second point in degrees.</param>
        /// <returns>The distance in meters between the two points.</returns>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Radius of the Earth in meters
            const double earthRadius = 6371000;
            
            // Convert degrees to radians
            double lat1Rad = lat1 * Math.PI / 180;
            double lon1Rad = lon1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;
            double lon2Rad = lon2 * Math.PI / 180;
            
            // Differences
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;
            
            // Haversine formula
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;
            
            return distance;
        }
    }
}