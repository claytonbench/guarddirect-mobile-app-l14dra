using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository implementation for Photo entities that provides data access operations
    /// including CRUD operations and specialized queries.
    /// </summary>
    public class PhotoRepository : IPhotoRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the PhotoRepository class with the specified database context.
        /// </summary>
        /// <param name="context">The database context used for data access operations.</param>
        public PhotoRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, containing the photo if found, otherwise null.</returns>
        public async Task<Photo> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            }

            return await _context.Photos
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Retrieves all photos for a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose photos to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of photos belonging to the specified user.</returns>
        public async Task<IEnumerable<Photo>> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            return await _context.Photos
                .Include(p => p.User)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a paginated list of photos for a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose photos to retrieve.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of photos per page.</param>
        /// <returns>A task that represents the asynchronous operation, containing a paginated list of photos.</returns>
        public async Task<PaginatedList<Photo>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (pageNumber < 1)
            {
                throw new ArgumentException("Page number must be greater than zero.", nameof(pageNumber));
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
            }

            var query = _context.Photos
                .Include(p => p.User)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Timestamp);

            return await PaginatedList<Photo>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <summary>
        /// Retrieves photos within a specified radius of a geographic location.
        /// Uses a two-step approach:
        /// 1. First, uses a bounding box for an efficient initial filter
        /// 2. Then, applies the Haversine formula for precise distance calculation
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point.</param>
        /// <param name="longitude">The longitude coordinate of the center point.</param>
        /// <param name="radiusInMeters">The radius in meters from the center point.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of photos within the specified radius.</returns>
        public async Task<IEnumerable<Photo>> GetByLocationAsync(double latitude, double longitude, double radiusInMeters)
        {
            // Validate parameters
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
                throw new ArgumentOutOfRangeException(nameof(radiusInMeters), "Radius must be greater than zero.");
            }

            // Convert radius from meters to degrees for the approximate bounding box
            // This conversion is approximate and varies by latitude
            // At the equator, 1 degree is approximately 111.32 km or 111,320 meters
            double radiusInDegrees = radiusInMeters / 111320;

            // Create a bounding box for efficient initial filtering
            double minLatitude = latitude - radiusInDegrees;
            double maxLatitude = latitude + radiusInDegrees;
            
            // Adjust for longitude which varies with latitude
            double longitudeDegreesPerMeter = 111320 * Math.Cos(latitude * Math.PI / 180) / 111320;
            double minLongitude = longitude - radiusInDegrees / longitudeDegreesPerMeter;
            double maxLongitude = longitude + radiusInDegrees / longitudeDegreesPerMeter;

            // Query photos within the bounding box first for efficiency
            var photos = await _context.Photos
                .Include(p => p.User)
                .Where(p => 
                    p.Latitude >= minLatitude && 
                    p.Latitude <= maxLatitude && 
                    p.Longitude >= minLongitude && 
                    p.Longitude <= maxLongitude)
                .ToListAsync();

            // Apply the Haversine formula to calculate the precise distance and filter
            // Earth's radius in meters
            const double earthRadius = 6371000;

            var result = photos
                .Select(p => new
                {
                    Photo = p,
                    Distance = CalculateHaversineDistance(
                        latitude, longitude, 
                        p.Latitude, p.Longitude, 
                        earthRadius)
                })
                .Where(p => p.Distance <= radiusInMeters)
                .OrderBy(p => p.Distance)
                .Select(p => p.Photo)
                .ToList();

            return result;
        }

        /// <summary>
        /// Calculates the great-circle distance between two points on the Earth using the Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of first point in degrees.</param>
        /// <param name="lon1">Longitude of first point in degrees.</param>
        /// <param name="lat2">Latitude of second point in degrees.</param>
        /// <param name="lon2">Longitude of second point in degrees.</param>
        /// <param name="earthRadius">Earth's radius in meters.</param>
        /// <returns>The distance between the points in meters.</returns>
        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2, double earthRadius)
        {
            // Convert degrees to radians
            lat1 = lat1 * Math.PI / 180;
            lon1 = lon1 * Math.PI / 180;
            lat2 = lat2 * Math.PI / 180;
            lon2 = lon2 * Math.PI / 180;

            // Haversine formula
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance;
        }

        /// <summary>
        /// Retrieves photos captured within a specified date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of photos captured within the date range.</returns>
        public async Task<IEnumerable<Photo>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException("Start date must be less than or equal to end date.", nameof(startDate));
            }

            // Set the end date to the end of the day for inclusive filtering
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            return await _context.Photos
                .Include(p => p.User)
                .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate)
                .OrderByDescending(p => p.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new photo to the database.
        /// </summary>
        /// <param name="photo">The photo entity to add.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result with the ID of the newly created photo.</returns>
        public async Task<Result<int>> AddAsync(Photo photo)
        {
            if (photo == null)
            {
                return Result.Failure<int>("Photo cannot be null.");
            }

            try
            {
                await _context.Photos.AddAsync(photo);
                await _context.SaveChangesAsync();

                return Result.Success(photo.Id, "Photo added successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception here if logging is implemented
                return Result.Failure<int>($"Failed to add photo: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing photo in the database.
        /// </summary>
        /// <param name="photo">The photo entity with updated values.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of the update operation.</returns>
        public async Task<Result> UpdateAsync(Photo photo)
        {
            if (photo == null)
            {
                return Result.Failure("Photo cannot be null.");
            }

            if (photo.Id <= 0)
            {
                return Result.Failure("Invalid photo ID.");
            }

            try
            {
                // Check if the photo exists
                bool exists = await _context.Photos.AnyAsync(p => p.Id == photo.Id);
                if (!exists)
                {
                    return Result.Failure($"Photo with ID {photo.Id} not found.");
                }

                _context.Entry(photo).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Result.Success("Photo updated successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception here if logging is implemented
                return Result.Failure($"Failed to update photo: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a photo from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to delete.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of the delete operation.</returns>
        public async Task<Result> DeleteAsync(int id)
        {
            if (id <= 0)
            {
                return Result.Failure("ID must be greater than zero.");
            }

            try
            {
                var photo = await _context.Photos.FindAsync(id);
                if (photo == null)
                {
                    return Result.Failure($"Photo with ID {id} not found.");
                }

                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();

                return Result.Success("Photo deleted successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception here if logging is implemented
                return Result.Failure($"Failed to delete photo: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a photo with the specified ID exists in the database.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to check.</param>
        /// <returns>A task that represents the asynchronous operation, containing true if the photo exists, otherwise false.</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _context.Photos.AnyAsync(p => p.Id == id);
        }

        /// <summary>
        /// Deletes photos older than the specified date.
        /// Implements the data retention policy requiring photos to be retained for 30 days.
        /// </summary>
        /// <param name="date">The cutoff date. Photos older than this date will be deleted.</param>
        /// <returns>A task that represents the asynchronous operation, containing the number of photos deleted.</returns>
        public async Task<int> DeleteOlderThanAsync(DateTime date)
        {
            var photosToDelete = await _context.Photos
                .Where(p => p.Timestamp < date)
                .ToListAsync();

            if (photosToDelete.Any())
            {
                _context.Photos.RemoveRange(photosToDelete);
                await _context.SaveChangesAsync();
            }

            return photosToDelete.Count;
        }
    }
}