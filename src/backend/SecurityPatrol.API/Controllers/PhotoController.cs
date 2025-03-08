using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// API controller that handles photo-related operations for the Security Patrol application,
    /// including uploading, retrieving, and managing photos captured by security personnel during patrols.
    /// </summary>
    [ApiController]
    [Route("api/v1/photos")]
    public class PhotoController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PhotoController> _logger;

        /// <summary>
        /// Initializes a new instance of the PhotoController class with required dependencies.
        /// </summary>
        /// <param name="photoService">Service for handling photo operations</param>
        /// <param name="currentUserService">Service for accessing current user information</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public PhotoController(
            IPhotoService photoService,
            ICurrentUserService currentUserService,
            ILogger<PhotoController> logger)
        {
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Uploads a photo with metadata to the system.
        /// </summary>
        /// <param name="request">The request containing photo metadata</param>
        /// <param name="file">The photo file to upload</param>
        /// <returns>A result containing the upload response with ID and status if successful</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(Result<PhotoUploadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [Authorize]
        public async Task<ActionResult<Result<PhotoUploadResponse>>> Upload([FromForm] PhotoUploadRequest request, IFormFile file)
        {
            _logger.LogInformation("Photo upload requested by user {UserId}", _currentUserService.UserId);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Photo upload failed: No file provided");
                return BadRequest(new Result<PhotoUploadResponse> { Success = false, Message = "No file provided" });
            }

            try
            {
                // Set the user ID from the authenticated user
                request.UserId = _currentUserService.UserId;

                using (var stream = file.OpenReadStream())
                {
                    var result = await _photoService.UploadPhotoAsync(request, stream);
                    _logger.LogInformation("Photo successfully uploaded with ID: {PhotoId}", result.Id);
                    return Ok(new Result<PhotoUploadResponse>
                    {
                        Success = true,
                        Data = result,
                        Message = "Photo uploaded successfully"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo");
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Retrieves a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The photo ID</param>
        /// <returns>A result containing the photo if found</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<Photo>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<Result<Photo>>> GetById(int id)
        {
            _logger.LogInformation("Photo retrieval requested for ID: {PhotoId}", id);

            try
            {
                var photo = await _photoService.GetPhotoAsync(id);
                if (photo == null)
                {
                    _logger.LogWarning("Photo with ID {PhotoId} not found", id);
                    return NotFound(new Result<Photo> { Success = false, Message = "Photo not found" });
                }

                _logger.LogInformation("Photo with ID {PhotoId} retrieved successfully", id);
                return Ok(new Result<Photo>
                {
                    Success = true,
                    Data = photo,
                    Message = "Photo retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photo with ID {PhotoId}", id);
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Retrieves the binary data of a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The photo ID</param>
        /// <returns>The photo file as a FileStreamResult if found</returns>
        [HttpGet("{id}/file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult> GetPhotoFile(int id)
        {
            _logger.LogInformation("Photo file retrieval requested for ID: {PhotoId}", id);

            try
            {
                var (stream, contentType, fileName) = await _photoService.GetPhotoStreamAsync(id);
                if (stream == null)
                {
                    _logger.LogWarning("Photo file with ID {PhotoId} not found", id);
                    return NotFound(new { Success = false, Message = "Photo file not found" });
                }

                _logger.LogInformation("Photo file with ID {PhotoId} retrieved successfully", id);
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photo file with ID {PhotoId}", id);
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Retrieves all photos for the current authenticated user.
        /// </summary>
        /// <returns>A result containing a collection of photos belonging to the current user</returns>
        [HttpGet("my")]
        [ProducesResponseType(typeof(Result<IEnumerable<Photo>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<ActionResult<Result<IEnumerable<Photo>>>> GetMyPhotos()
        {
            var userId = _currentUserService.UserId;
            _logger.LogInformation("Retrieving all photos for user {UserId}", userId);

            try
            {
                var photos = await _photoService.GetPhotosByUserIdAsync(userId);
                _logger.LogInformation("Retrieved {Count} photos for user {UserId}", photos.Count, userId);

                return Ok(new Result<IEnumerable<Photo>>
                {
                    Success = true,
                    Data = photos,
                    Message = "Photos retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photos for user {UserId}", userId);
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Retrieves a paginated list of photos for the current authenticated user.
        /// </summary>
        /// <param name="pageNumber">The page number</param>
        /// <param name="pageSize">The page size</param>
        /// <returns>A result containing a paginated list of photos belonging to the current user</returns>
        [HttpGet("my/paginated")]
        [ProducesResponseType(typeof(Result<PaginatedList<Photo>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<ActionResult<Result<PaginatedList<Photo>>>> GetMyPhotosPaginated(int pageNumber = 1, int pageSize = 10)
        {
            var userId = _currentUserService.UserId;
            _logger.LogInformation("Retrieving paginated photos for user {UserId} (Page {PageNumber}, Size {PageSize})", 
                userId, pageNumber, pageSize);

            try
            {
                var photos = await _photoService.GetPaginatedPhotosByUserIdAsync(userId, pageNumber, pageSize);
                _logger.LogInformation("Retrieved page {PageNumber} with {Count} photos for user {UserId}",
                    pageNumber, photos.Items.Count, userId);

                return Ok(new Result<PaginatedList<Photo>>
                {
                    Success = true,
                    Data = photos,
                    Message = "Paginated photos retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated photos for user {UserId}", userId);
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Retrieves photos within a specified radius of a geographic location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate</param>
        /// <param name="longitude">The longitude coordinate</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of photos within the specified radius of the location</returns>
        [HttpGet("location")]
        [ProducesResponseType(typeof(Result<IEnumerable<Photo>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [Authorize]
        public async Task<ActionResult<Result<IEnumerable<Photo>>>> GetPhotosByLocation(
            double latitude, double longitude, double radiusInMeters)
        {
            _logger.LogInformation("Retrieving photos near location ({Latitude}, {Longitude}) within {Radius} meters",
                latitude, longitude, radiusInMeters);

            // Validate parameters
            if (latitude < -90 || latitude > 90)
            {
                _logger.LogWarning("Invalid latitude value: {Latitude}", latitude);
                return BadRequest(new Result<IEnumerable<Photo>> 
                { 
                    Success = false, 
                    Message = "Latitude must be between -90 and 90 degrees" 
                });
            }

            if (longitude < -180 || longitude > 180)
            {
                _logger.LogWarning("Invalid longitude value: {Longitude}", longitude);
                return BadRequest(new Result<IEnumerable<Photo>> 
                { 
                    Success = false, 
                    Message = "Longitude must be between -180 and 180 degrees" 
                });
            }

            if (radiusInMeters <= 0)
            {
                _logger.LogWarning("Invalid radius value: {Radius}", radiusInMeters);
                return BadRequest(new Result<IEnumerable<Photo>> 
                { 
                    Success = false, 
                    Message = "Radius must be greater than 0" 
                });
            }

            try
            {
                var photos = await _photoService.GetPhotosByLocationAsync(latitude, longitude, radiusInMeters);
                _logger.LogInformation("Retrieved {Count} photos near location ({Latitude}, {Longitude})",
                    photos.Count, latitude, longitude);

                return Ok(new Result<IEnumerable<Photo>>
                {
                    Success = true,
                    Data = photos,
                    Message = "Location-based photos retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photos by location");
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Retrieves photos captured within a specified date range.
        /// </summary>
        /// <param name="startDate">The start date (inclusive)</param>
        /// <param name="endDate">The end date (inclusive)</param>
        /// <returns>A result containing a collection of photos captured within the specified date range</returns>
        [HttpGet("daterange")]
        [ProducesResponseType(typeof(Result<IEnumerable<Photo>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [Authorize]
        public async Task<ActionResult<Result<IEnumerable<Photo>>>> GetPhotosByDateRange(
            DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Retrieving photos between {StartDate} and {EndDate}", 
                startDate, endDate);

            // Validate parameters
            if (startDate > endDate)
            {
                _logger.LogWarning("Invalid date range: {StartDate} is after {EndDate}", startDate, endDate);
                return BadRequest(new Result<IEnumerable<Photo>> 
                { 
                    Success = false, 
                    Message = "Start date must be before or equal to end date" 
                });
            }

            try
            {
                var photos = await _photoService.GetPhotosByDateRangeAsync(startDate, endDate);
                _logger.LogInformation("Retrieved {Count} photos between {StartDate} and {EndDate}",
                    photos.Count, startDate, endDate);

                return Ok(new Result<IEnumerable<Photo>>
                {
                    Success = true,
                    Data = photos,
                    Message = "Date range photos retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving photos by date range");
                throw; // Let the exception filter handle it
            }
        }

        /// <summary>
        /// Deletes a photo by its unique identifier.
        /// </summary>
        /// <param name="id">The photo ID</param>
        /// <returns>A result indicating success or failure of the delete operation</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<Result>> DeletePhoto(int id)
        {
            _logger.LogInformation("Photo deletion requested for ID: {PhotoId}", id);

            try
            {
                var result = await _photoService.DeletePhotoAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Photo with ID {PhotoId} not found for deletion", id);
                    return NotFound(new Result { Success = false, Message = "Photo not found" });
                }

                _logger.LogInformation("Photo with ID {PhotoId} deleted successfully", id);
                return Ok(new Result
                {
                    Success = true,
                    Message = "Photo deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo with ID {PhotoId}", id);
                throw; // Let the exception filter handle it
            }
        }
    }
}