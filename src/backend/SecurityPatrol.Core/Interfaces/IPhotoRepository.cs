using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the interface for a repository that provides data access operations for Photo entities.
    /// </summary>
    public interface IPhotoRepository
    {
        /// <summary>
        /// Retrieves a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, containing the photo if found, otherwise null.</returns>
        Task<Photo> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all photos for a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose photos to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of photos belonging to the specified user.</returns>
        Task<IEnumerable<Photo>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Retrieves a paginated list of photos for a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose photos to retrieve.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of photos per page.</param>
        /// <returns>A task that represents the asynchronous operation, containing a paginated list of photos.</returns>
        Task<PaginatedList<Photo>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves photos within a specified radius of a geographic location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point.</param>
        /// <param name="longitude">The longitude coordinate of the center point.</param>
        /// <param name="radiusInMeters">The radius in meters from the center point.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of photos within the specified radius.</returns>
        Task<IEnumerable<Photo>> GetByLocationAsync(double latitude, double longitude, double radiusInMeters);

        /// <summary>
        /// Retrieves photos captured within a specified date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of photos captured within the date range.</returns>
        Task<IEnumerable<Photo>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Adds a new photo to the repository.
        /// </summary>
        /// <param name="photo">The photo entity to add.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result with the ID of the newly added photo.</returns>
        Task<Result<int>> AddAsync(Photo photo);

        /// <summary>
        /// Updates an existing photo in the repository.
        /// </summary>
        /// <param name="photo">The photo entity with updated values.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of the update operation.</returns>
        Task<Result> UpdateAsync(Photo photo);

        /// <summary>
        /// Deletes a photo from the repository.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to delete.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of the delete operation.</returns>
        Task<Result> DeleteAsync(int id);

        /// <summary>
        /// Checks if a photo with the specified ID exists in the repository.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to check.</param>
        /// <returns>A task that represents the asynchronous operation, containing true if the photo exists, otherwise false.</returns>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Deletes photos older than the specified date.
        /// Implements the data retention policy requiring photos to be retained for 30 days.
        /// </summary>
        /// <param name="date">The cutoff date. Photos older than this date will be deleted.</param>
        /// <returns>A task that represents the asynchronous operation, containing the number of photos deleted.</returns>
        Task<int> DeleteOlderThanAsync(DateTime date);
    }
}