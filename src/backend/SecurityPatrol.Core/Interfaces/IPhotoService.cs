using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the interface for a service that handles photo management operations
    /// in the Security Patrol application.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Uploads a photo with the provided metadata and binary data.
        /// </summary>
        /// <param name="request">The metadata for the photo being uploaded.</param>
        /// <param name="photoStream">The binary stream containing the photo data.</param>
        /// <param name="contentType">The MIME content type of the photo (e.g., "image/jpeg").</param>
        /// <returns>A result containing the upload response with ID and status if successful.</returns>
        Task<Result<PhotoUploadResponse>> UploadPhotoAsync(PhotoUploadRequest request, Stream photoStream, string contentType);

        /// <summary>
        /// Retrieves a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A result containing the photo if found.</returns>
        Task<Result<Photo>> GetPhotoAsync(int id);

        /// <summary>
        /// Retrieves the binary data of a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo.</param>
        /// <returns>A result containing the photo binary data if found.</returns>
        Task<Result<Stream>> GetPhotoStreamAsync(int id);

        /// <summary>
        /// Retrieves all photos for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A result containing a collection of photos belonging to the specified user.</returns>
        Task<Result<IEnumerable<Photo>>> GetPhotosByUserIdAsync(string userId);

        /// <summary>
        /// Retrieves a paginated list of photos for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of photos per page.</param>
        /// <returns>A result containing a paginated list of photos belonging to the specified user.</returns>
        Task<Result<PaginatedList<Photo>>> GetPaginatedPhotosByUserIdAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves photos within a specified radius of a geographic location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate of the center point.</param>
        /// <param name="longitude">The longitude coordinate of the center point.</param>
        /// <param name="radiusInMeters">The search radius in meters.</param>
        /// <returns>A result containing a collection of photos within the specified radius of the location.</returns>
        Task<Result<IEnumerable<Photo>>> GetPhotosByLocationAsync(double latitude, double longitude, double radiusInMeters);

        /// <summary>
        /// Retrieves photos captured within a specified date range.
        /// </summary>
        /// <param name="startDate">The start date and time of the range.</param>
        /// <param name="endDate">The end date and time of the range.</param>
        /// <returns>A result containing a collection of photos captured within the specified date range.</returns>
        Task<Result<IEnumerable<Photo>>> GetPhotosByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Deletes a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the photo to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        Task<Result> DeletePhotoAsync(int id);
    }
}