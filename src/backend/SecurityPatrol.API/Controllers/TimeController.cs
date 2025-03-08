using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// Controller that provides API endpoints for time tracking operations in the Security Patrol application.
    /// </summary>
    [ApiController]
    [Route("api/time")]
    [Authorize]
    public class TimeController : ControllerBase
    {
        private readonly ITimeRecordService _timeRecordService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<TimeController> _logger;

        /// <summary>
        /// Initializes a new instance of the TimeController with required dependencies.
        /// </summary>
        /// <param name="timeRecordService">Service for time record operations</param>
        /// <param name="currentUserService">Service to access current user information</param>
        /// <param name="logger">Logger for the TimeController</param>
        public TimeController(
            ITimeRecordService timeRecordService,
            ICurrentUserService currentUserService,
            ILogger<TimeController> logger)
        {
            _timeRecordService = timeRecordService ?? throw new ArgumentNullException(nameof(timeRecordService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Endpoint for recording clock in/out events.
        /// </summary>
        /// <param name="request">The time record request containing type, timestamp, and location</param>
        /// <returns>Action result containing the response for the clock operation</returns>
        [HttpPost]
        [ProducesResponseType(typeof(TimeRecordResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Clock([FromBody] TimeRecordRequest request)
        {
            try
            {
                _logger.LogInformation("Clock operation requested");

                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                var userId = _currentUserService.GetUserId();
                if (!_currentUserService.IsAuthenticated() || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _timeRecordService.CreateTimeRecordAsync(request, userId);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Clock operation successful for user {UserId}", userId);
                    return Ok(result.Data);
                }
                else
                {
                    _logger.LogWarning("Clock operation failed for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during clock operation");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Endpoint for retrieving time record history with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based)</param>
        /// <param name="pageSize">The number of records per page</param>
        /// <returns>Action result containing the paginated time record history</returns>
        [HttpGet("history")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("GetHistory operation requested");

                if (pageNumber < 1)
                {
                    pageNumber = 1;
                }

                if (pageSize < 1)
                {
                    pageSize = 10;
                }

                var userId = _currentUserService.GetUserId();
                if (!_currentUserService.IsAuthenticated() || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _timeRecordService.GetTimeRecordHistoryAsync(userId, pageNumber, pageSize);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("GetHistory operation successful for user {UserId}", userId);
                    return Ok(result.Data);
                }
                else
                {
                    _logger.LogWarning("GetHistory operation failed for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetHistory operation");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Endpoint for retrieving the current clock status (in/out) of the user.
        /// </summary>
        /// <returns>Action result containing the current clock status</returns>
        [HttpGet("status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                _logger.LogInformation("GetStatus operation requested");

                var userId = _currentUserService.GetUserId();
                if (!_currentUserService.IsAuthenticated() || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _timeRecordService.GetCurrentStatusAsync(userId);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("GetStatus operation successful for user {UserId}", userId);
                    return Ok(result.Data);
                }
                else
                {
                    _logger.LogWarning("GetStatus operation failed for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetStatus operation");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Endpoint for retrieving time records within a specified date range.
        /// </summary>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <returns>Action result containing the time records within the date range</returns>
        [HttpGet("range")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                _logger.LogInformation("GetByDateRange operation requested");

                if (startDate == default)
                {
                    return BadRequest("Start date must be provided");
                }

                if (endDate == default)
                {
                    return BadRequest("End date must be provided");
                }

                if (startDate > endDate)
                {
                    return BadRequest("Start date must be earlier than or equal to end date");
                }

                var userId = _currentUserService.GetUserId();
                if (!_currentUserService.IsAuthenticated() || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _timeRecordService.GetTimeRecordsByDateRangeAsync(userId, startDate, endDate);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("GetByDateRange operation successful for user {UserId}", userId);
                    return Ok(result.Data);
                }
                else
                {
                    _logger.LogWarning("GetByDateRange operation failed for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GetByDateRange operation");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }
    }
}