using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// Controller that handles location data operations for the Security Patrol application.
    /// Provides endpoints for submitting location batches from mobile clients, retrieving location history,
    /// and accessing the latest location information for security personnel.
    /// </summary>
    [ApiController]
    [Route("api/v1/location")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<LocationController> _logger;

        /// <summary>
        /// Initializes a new instance of the LocationController with required dependencies.
        /// </summary>
        /// <param name="locationService">Service for location data operations</param>
        /// <param name="currentUserService">Service for accessing current user information</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public LocationController(
            ILocationService locationService,
            ICurrentUserService currentUserService,
            ILogger<LocationController> logger)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles batch submission of location data points from mobile clients.
        /// </summary>
        /// <param name="request">The batch of location data to process</param>
        /// <returns>A response indicating which location records were successfully processed</returns>
        [HttpPost("batch")]
        [ProducesResponseType(typeof(LocationSyncResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> BatchAsync([FromBody] LocationBatchRequest request)
        {
            _logger.LogInformation("Location batch request received");
            
            if (!_currentUserService.IsAuthenticated())
            {
                _logger.LogWarning("Unauthorized access attempt to location batch endpoint");
                return Unauthorized();
            }
            
            if (request == null)
            {
                return BadRequest("Request cannot be null");
            }

            // If the userId is not set, use the current user's id
            if (string.IsNullOrEmpty(request.UserId))
            {
                request.UserId = _currentUserService.GetUserId();
                _logger.LogDebug("Setting request UserId to current user: {UserId}", request.UserId);
            }

            try
            {
                var response = await _locationService.ProcessLocationBatchAsync(request);
                _logger.LogInformation("Location batch processed. Synced: {SyncedCount}, Failed: {FailedCount}", 
                    response.GetSuccessCount(), response.GetFailureCount());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing location batch");
                return StatusCode(500, "An error occurred while processing the location batch");
            }
        }

        /// <summary>
        /// Retrieves location history for the authenticated user within a specific time range.
        /// </summary>
        /// <param name="startTime">The start of the time range (inclusive)</param>
        /// <param name="endTime">The end of the time range (inclusive)</param>
        /// <returns>Collection of location data points within the specified time range</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(IEnumerable<LocationModel>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetHistoryAsync([FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            _logger.LogInformation("Location history request received for period {StartTime} to {EndTime}", startTime, endTime);
            
            if (!_currentUserService.IsAuthenticated())
            {
                _logger.LogWarning("Unauthorized access attempt to location history endpoint");
                return Unauthorized();
            }
            
            string userId = _currentUserService.GetUserId();
            
            if (startTime >= endTime)
            {
                return BadRequest("Start time must be earlier than end time");
            }

            try
            {
                var history = await _locationService.GetLocationHistoryAsync(userId, startTime, endTime);
                _logger.LogInformation("Retrieved {Count} location records for user {UserId}", 
                    history is ICollection<LocationModel> collection ? collection.Count : -1, userId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location history for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving location history");
            }
        }

        /// <summary>
        /// Retrieves the latest location for the authenticated user.
        /// </summary>
        /// <returns>The most recent location data point for the authenticated user</returns>
        [HttpGet("current")]
        [ProducesResponseType(typeof(LocationModel), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCurrentAsync()
        {
            _logger.LogInformation("Current location request received");
            
            if (!_currentUserService.IsAuthenticated())
            {
                _logger.LogWarning("Unauthorized access attempt to current location endpoint");
                return Unauthorized();
            }
            
            string userId = _currentUserService.GetUserId();

            try
            {
                var location = await _locationService.GetLatestLocationAsync(userId);
                if (location == null)
                {
                    _logger.LogInformation("No location found for user {UserId}", userId);
                    return NoContent();
                }
                
                _logger.LogInformation("Retrieved current location for user {UserId}: Lat {Latitude}, Lng {Longitude}", 
                    userId, location.Latitude, location.Longitude);
                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current location for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving current location");
            }
        }

        /// <summary>
        /// Retrieves the latest location for a specific user. This endpoint is restricted to administrators.
        /// </summary>
        /// <param name="userId">The ID of the user whose location is being requested</param>
        /// <returns>The most recent location data point for the specified user</returns>
        [HttpGet("users/{userId}/current")]
        [ProducesResponseType(typeof(LocationModel), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetCurrentByUserIdAsync(string userId)
        {
            _logger.LogInformation("User-specific current location request received for user {UserId}", userId);
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID cannot be null or empty");
            }

            try
            {
                var location = await _locationService.GetLatestLocationAsync(userId);
                if (location == null)
                {
                    _logger.LogInformation("No location found for user {UserId}", userId);
                    return NoContent();
                }
                
                _logger.LogInformation("Retrieved current location for user {UserId}: Lat {Latitude}, Lng {Longitude}", 
                    userId, location.Latitude, location.Longitude);
                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current location for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving current location");
            }
        }
    }
}