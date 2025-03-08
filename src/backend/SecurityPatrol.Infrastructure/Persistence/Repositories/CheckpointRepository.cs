using Microsoft.EntityFrameworkCore; // v8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of the ICheckpointRepository interface that provides data access operations for Checkpoint entities using Entity Framework Core.
    /// </summary>
    public class CheckpointRepository : ICheckpointRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the CheckpointRepository class with the specified database context.
        /// </summary>
        /// <param name="context">The database context to use for checkpoint operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if the context is null.</exception>
        public CheckpointRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a checkpoint by its unique identifier.
        /// </summary>
        /// <param name="id">The checkpoint ID to retrieve.</param>
        /// <returns>The checkpoint with the specified ID, or null if not found.</returns>
        public async Task<Checkpoint> GetByIdAsync(int id)
        {
            return await _context.Checkpoints.FindAsync(id);
        }

        /// <summary>
        /// Retrieves all checkpoints associated with a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A collection of checkpoints for the specified location.</returns>
        public async Task<IEnumerable<Checkpoint>> GetByLocationIdAsync(int locationId)
        {
            return await _context.Checkpoints
                .Where(c => c.LocationId == locationId)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all checkpoints in the system.
        /// </summary>
        /// <returns>A collection of all checkpoints.</returns>
        public async Task<IEnumerable<Checkpoint>> GetAllAsync()
        {
            return await _context.Checkpoints.ToListAsync();
        }

        /// <summary>
        /// Adds a new checkpoint to the system.
        /// </summary>
        /// <param name="checkpoint">The checkpoint entity to add.</param>
        /// <returns>A result containing the ID of the newly created checkpoint if successful.</returns>
        public async Task<Result<int>> AddAsync(Checkpoint checkpoint)
        {
            if (checkpoint == null)
            {
                return Result.Failure<int>("Checkpoint cannot be null");
            }

            if (string.IsNullOrWhiteSpace(checkpoint.Name))
            {
                return Result.Failure<int>("Checkpoint name is required");
            }

            if (checkpoint.LocationId <= 0)
            {
                return Result.Failure<int>("Valid patrol location ID is required");
            }

            try
            {
                // Ensure LastUpdated is set to current time
                checkpoint.LastUpdated = DateTime.UtcNow;
                
                await _context.Checkpoints.AddAsync(checkpoint);
                await _context.SaveChangesAsync();
                
                return Result.Success(checkpoint.Id, "Checkpoint created successfully");
            }
            catch (Exception ex)
            {
                return Result.Failure<int>($"Error creating checkpoint: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing checkpoint in the system.
        /// </summary>
        /// <param name="checkpoint">The checkpoint entity with updated values.</param>
        /// <returns>A result indicating success or failure of the update operation.</returns>
        public async Task<Result> UpdateAsync(Checkpoint checkpoint)
        {
            if (checkpoint == null)
            {
                return Result.Failure("Checkpoint cannot be null");
            }

            if (checkpoint.Id <= 0)
            {
                return Result.Failure("Invalid checkpoint ID");
            }

            if (string.IsNullOrWhiteSpace(checkpoint.Name))
            {
                return Result.Failure("Checkpoint name is required");
            }

            if (checkpoint.LocationId <= 0)
            {
                return Result.Failure("Valid patrol location ID is required");
            }

            try
            {
                bool exists = await ExistsAsync(checkpoint.Id);
                if (!exists)
                {
                    return Result.Failure($"Checkpoint with ID {checkpoint.Id} not found");
                }

                checkpoint.LastUpdated = DateTime.UtcNow;
                _context.Checkpoints.Update(checkpoint);
                await _context.SaveChangesAsync();
                
                return Result.Success("Checkpoint updated successfully");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error updating checkpoint: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a checkpoint from the system.
        /// </summary>
        /// <param name="id">The ID of the checkpoint to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var checkpoint = await GetByIdAsync(id);
                if (checkpoint == null)
                {
                    return Result.Failure($"Checkpoint with ID {id} not found");
                }

                // Check if there are any checkpoint verifications that reference this checkpoint
                bool hasVerifications = await _context.CheckpointVerifications
                    .AnyAsync(v => v.CheckpointId == id);

                if (hasVerifications)
                {
                    return Result.Failure("Cannot delete checkpoint with existing verifications. Delete verifications first or archive the checkpoint instead.");
                }

                _context.Checkpoints.Remove(checkpoint);
                await _context.SaveChangesAsync();
                
                return Result.Success("Checkpoint deleted successfully");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error deleting checkpoint: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves checkpoints within a specified distance of a given location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point.</param>
        /// <param name="longitude">The longitude coordinate of the center point.</param>
        /// <param name="radiusInMeters">The radius in meters to search within.</param>
        /// <returns>A collection of checkpoints within the specified radius.</returns>
        public async Task<IEnumerable<Checkpoint>> GetNearbyCheckpointsAsync(double latitude, double longitude, double radiusInMeters)
        {
            // Convert radius from meters to approximate degrees for latitude and longitude
            // This is a rough approximation to create a bounding box for initial filtering
            // At the equator, 1 degree of latitude is approximately 111,320 meters
            // Longitude degrees vary based on latitude
            double latDegrees = radiusInMeters / 111320.0;
            double lonDegrees = radiusInMeters / (111320.0 * Math.Cos(latitude * (Math.PI / 180)));

            // Create a bounding box for the initial query to optimize the search
            double minLat = latitude - latDegrees;
            double maxLat = latitude + latDegrees;
            double minLon = longitude - lonDegrees;
            double maxLon = longitude + lonDegrees;

            // First, get all checkpoints within the bounding box as an initial filter
            var checkpointsInBox = await _context.Checkpoints
                .Where(c => c.Latitude >= minLat && c.Latitude <= maxLat
                       && c.Longitude >= minLon && c.Longitude <= maxLon)
                .ToListAsync();

            // Then, filter out points that are outside the radius using the Haversine formula
            var nearbyCheckpoints = checkpointsInBox
                .Where(c => CalculateDistance(latitude, longitude, c.Latitude, c.Longitude) <= radiusInMeters)
                .ToList();

            return nearbyCheckpoints;
        }

        /// <summary>
        /// Checks if a checkpoint with the specified ID exists in the system.
        /// </summary>
        /// <param name="id">The checkpoint ID to check.</param>
        /// <returns>True if the checkpoint exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Checkpoints.AnyAsync(c => c.Id == id);
        }

        /// <summary>
        /// Calculates the distance between two geographic coordinates using the Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of the first point in degrees.</param>
        /// <param name="lon1">Longitude of the first point in degrees.</param>
        /// <param name="lat2">Latitude of the second point in degrees.</param>
        /// <param name="lon2">Longitude of the second point in degrees.</param>
        /// <returns>The distance in meters between the two points.</returns>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth's approximate radius in meters
            const double EarthRadius = 6371000;

            // Convert degrees to radians
            double lat1Rad = lat1 * (Math.PI / 180);
            double lon1Rad = lon1 * (Math.PI / 180);
            double lat2Rad = lat2 * (Math.PI / 180);
            double lon2Rad = lon2 * (Math.PI / 180);

            // Calculate differences
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            // Haversine formula
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            double distance = EarthRadius * c;

            return distance;
        }
    }
}