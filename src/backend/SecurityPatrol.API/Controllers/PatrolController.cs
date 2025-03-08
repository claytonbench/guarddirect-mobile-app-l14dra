using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.API.Filters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// API controller that handles patrol management operations for the Security Patrol application,
    /// including retrieving patrol locations, managing checkpoints, and processing checkpoint verifications.
    /// </summary>
    [ApiController]
    [Route("api/v1/patrol")]
    [ServiceFilter(typeof(ApiExceptionFilter))]
    public class PatrolController : ControllerBase
    {
        private readonly IPatrolService _patrolService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PatrolController> _logger;

        /// <summary>
        /// Initializes a new instance of the PatrolController class with required dependencies.
        /// </summary>
        /// <param name="patrolService">Service that provides patrol management operations</param>
        /// <param name="currentUserService">Service that provides access to the current authenticated user</param>
        /// <param name="logger">Logger for recording controller operations</param>
        public PatrolController(
            IPatrolService patrolService,
            ICurrentUserService currentUserService,
            ILogger<PatrolController> logger)
        {
            _patrolService = patrolService ?? throw new ArgumentNullException(nameof(patrolService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all patrol locations available in the system.
        /// </summary>
        /// <returns>A result containing a collection of patrol locations if successful, or error details if the request fails.</returns>
        [HttpGet("locations")]
        [ProducesResponseType(typeof(Result<IEnumerable<PatrolLocation>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<IEnumerable<PatrolLocation>>>> GetLocations()
        {
            _logger.LogInformation("Retrieving all patrol locations");
            var result = await _patrolService.GetLocationsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific patrol location by its ID.
        /// </summary>
        /// <param name="id">The ID of the patrol location to retrieve</param>
        /// <returns>A result containing the patrol location if found, or error details if the request fails.</returns>
        [HttpGet("locations/{id}")]
        [ProducesResponseType(typeof(Result<PatrolLocation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Result<PatrolLocation>>> GetLocationById(int id)
        {
            _logger.LogInformation("Retrieving patrol location with ID: {LocationId}", id);
            var result = await _patrolService.GetLocationByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <returns>A result containing a collection of checkpoint models if successful, or error details if the request fails.</returns>
        [HttpGet("locations/{locationId}/checkpoints")]
        [ProducesResponseType(typeof(Result<IEnumerable<CheckpointModel>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Result<IEnumerable<CheckpointModel>>>> GetCheckpointsByLocationId(int locationId)
        {
            _logger.LogInformation("Retrieving checkpoints for location ID: {LocationId}", locationId);
            var result = await _patrolService.GetCheckpointsByLocationIdAsync(locationId);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific checkpoint by its ID.
        /// </summary>
        /// <param name="id">The ID of the checkpoint to retrieve</param>
        /// <returns>A result containing the checkpoint model if found, or error details if the request fails.</returns>
        [HttpGet("checkpoints/{id}")]
        [ProducesResponseType(typeof(Result<CheckpointModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Result<CheckpointModel>>> GetCheckpointById(int id)
        {
            _logger.LogInformation("Retrieving checkpoint with ID: {CheckpointId}", id);
            var result = await _patrolService.GetCheckpointByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Processes a checkpoint verification request from a security officer.
        /// </summary>
        /// <param name="request">The verification request containing checkpoint and location information</param>
        /// <returns>A result containing the verification response if successful, or error details if the request fails.</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(Result<CheckpointVerificationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Result<CheckpointVerificationResponse>>> VerifyCheckpoint(CheckpointVerificationRequest request)
        {
            _logger.LogInformation("Processing checkpoint verification for checkpoint ID: {CheckpointId}", request?.CheckpointId);
            
            if (request == null)
            {
                return BadRequest("Verification request cannot be null");
            }
            
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }
            
            var result = await _patrolService.VerifyCheckpointAsync(request, userId);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the current status of a patrol for a specific location and the current user.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <returns>A result containing the patrol status model if successful, or error details if the request fails.</returns>
        [HttpGet("locations/{locationId}/status")]
        [ProducesResponseType(typeof(Result<PatrolStatusModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Result<PatrolStatusModel>>> GetPatrolStatus(int locationId)
        {
            _logger.LogInformation("Retrieving patrol status for location ID: {LocationId}", locationId);
            
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }
            
            var result = await _patrolService.GetPatrolStatusAsync(locationId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves checkpoints that are within a specified distance of the user's current location.
        /// </summary>
        /// <param name="latitude">The latitude of the user's current position</param>
        /// <param name="longitude">The longitude of the user's current position</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of nearby checkpoint models if successful, or error details if the request fails.</returns>
        [HttpGet("checkpoints/nearby")]
        [ProducesResponseType(typeof(Result<IEnumerable<CheckpointModel>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<IEnumerable<CheckpointModel>>>> GetNearbyCheckpoints(double latitude, double longitude, double radiusInMeters)
        {
            _logger.LogInformation("Retrieving checkpoints near location: Lat={Latitude}, Long={Longitude}, Radius={Radius}m", 
                latitude, longitude, radiusInMeters);
            
            // Validate coordinate parameters
            if (latitude < -90 || latitude > 90)
            {
                return BadRequest("Latitude must be between -90 and 90 degrees");
            }
            
            if (longitude < -180 || longitude > 180)
            {
                return BadRequest("Longitude must be between -180 and 180 degrees");
            }
            
            if (radiusInMeters <= 0)
            {
                return BadRequest("Radius must be greater than 0 meters");
            }
            
            var result = await _patrolService.GetNearbyCheckpointsAsync(latitude, longitude, radiusInMeters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all checkpoint verifications for the current user.
        /// </summary>
        /// <returns>A result containing a collection of checkpoint verifications if successful, or error details if the request fails.</returns>
        [HttpGet("verifications")]
        [ProducesResponseType(typeof(Result<IEnumerable<CheckpointVerification>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<IEnumerable<CheckpointVerification>>>> GetUserVerifications()
        {
            _logger.LogInformation("Retrieving verification history for current user");
            
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }
            
            var result = await _patrolService.GetUserVerificationsAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves checkpoint verifications for the current user within a date range.
        /// </summary>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <returns>A result containing a collection of checkpoint verifications if successful, or error details if the request fails.</returns>
        [HttpGet("verifications/daterange")]
        [ProducesResponseType(typeof(Result<IEnumerable<CheckpointVerification>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<IEnumerable<CheckpointVerification>>>> GetUserVerificationsByDateRange(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Retrieving verification history for current user from {StartDate} to {EndDate}", 
                startDate, endDate);
            
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }
            
            // Validate date parameters
            if (startDate == default)
            {
                return BadRequest("Start date is required");
            }
            
            if (endDate == default || endDate < startDate)
            {
                return BadRequest("End date must be valid and greater than or equal to start date");
            }
            
            var result = await _patrolService.GetUserVerificationsByDateRangeAsync(userId, startDate, endDate);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves patrol locations that are within a specified distance of the user's current location.
        /// </summary>
        /// <param name="latitude">The latitude of the user's current position</param>
        /// <param name="longitude">The longitude of the user's current position</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of nearby patrol locations if successful, or error details if the request fails.</returns>
        [HttpGet("locations/nearby")]
        [ProducesResponseType(typeof(Result<IEnumerable<PatrolLocation>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Result<IEnumerable<PatrolLocation>>>> GetNearbyLocations(double latitude, double longitude, double radiusInMeters)
        {
            _logger.LogInformation("Retrieving patrol locations near location: Lat={Latitude}, Long={Longitude}, Radius={Radius}m", 
                latitude, longitude, radiusInMeters);
            
            // Validate coordinate parameters
            if (latitude < -90 || latitude > 90)
            {
                return BadRequest("Latitude must be between -90 and 90 degrees");
            }
            
            if (longitude < -180 || longitude > 180)
            {
                return BadRequest("Longitude must be between -180 and 180 degrees");
            }
            
            if (radiusInMeters <= 0)
            {
                return BadRequest("Radius must be greater than 0 meters");
            }
            
            var result = await _patrolService.GetNearbyLocationsAsync(latitude, longitude, radiusInMeters);
            return Ok(result);
        }

        /// <summary>
        /// Checks if a specific checkpoint has been verified by the current user.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <returns>A result containing a boolean indicating if the checkpoint is verified if successful, or error details if the request fails.</returns>
        [HttpGet("checkpoints/{checkpointId}/verified")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Result<bool>>> IsCheckpointVerified(int checkpointId)
        {
            _logger.LogInformation("Checking verification status for checkpoint ID: {CheckpointId}", checkpointId);
            
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }
            
            var result = await _patrolService.IsCheckpointVerifiedAsync(checkpointId, userId);
            return Ok(result);
        }
    }
}