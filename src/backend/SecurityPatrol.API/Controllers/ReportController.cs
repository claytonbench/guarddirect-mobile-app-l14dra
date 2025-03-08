using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.API.Controllers
{
    /// <summary>
    /// Controller that handles activity reporting operations for the Security Patrol application.
    /// Provides endpoints for creating, retrieving, updating, and deleting activity reports.
    /// </summary>
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReportController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportController"/> class.
        /// </summary>
        /// <param name="reportService">The service for report operations.</param>
        /// <param name="currentUserService">The service for current user information.</param>
        /// <param name="logger">The logger instance.</param>
        public ReportController(
            IReportService reportService,
            ICurrentUserService currentUserService,
            ILogger<ReportController> logger)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new activity report.
        /// </summary>
        /// <param name="request">The report request containing text, timestamp, and location.</param>
        /// <returns>A response containing the ID and status of the created report.</returns>
        /// <response code="200">Returns the created report details.</response>
        /// <response code="400">If the request is invalid or validation fails.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ReportResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateReport(ReportRequest request)
        {
            if (request == null)
            {
                return BadRequest("Report request cannot be null");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Report text cannot be empty");
            }

            if (request.Text.Length > 500)
            {
                return BadRequest("Report text exceeds maximum length of 500 characters");
            }

            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            try
            {
                _logger.LogInformation("Creating report for user {UserId}", userId);
                var result = await _reportService.CreateReportAsync(request, userId);

                if (result.Succeeded)
                {
                    return Ok(result.Data);
                }

                _logger.LogWarning("Failed to create report: {Message}", result.Message);
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report for user {UserId}", userId);
                return StatusCode(500, "An error occurred while creating the report");
            }
        }

        /// <summary>
        /// Retrieves paginated activity reports for the current user.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (defaults to 1).</param>
        /// <param name="pageSize">The number of reports per page (defaults to 10).</param>
        /// <returns>A paginated list of activity reports.</returns>
        /// <response code="200">Returns the paginated list of reports.</response>
        /// <response code="400">If the pagination parameters are invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<Report>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            try
            {
                _logger.LogInformation("Retrieving reports for user {UserId}, page {Page}, size {Size}", 
                    userId, pageNumber, pageSize);
                
                var result = await _reportService.GetPaginatedReportsByUserIdAsync(userId, pageNumber, pageSize);

                if (result.Succeeded)
                {
                    return Ok(result.Data);
                }

                _logger.LogWarning("Failed to retrieve reports: {Message}", result.Message);
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving reports");
            }
        }

        /// <summary>
        /// Retrieves a specific activity report by ID.
        /// </summary>
        /// <param name="id">The ID of the report to retrieve.</param>
        /// <returns>The requested activity report.</returns>
        /// <response code="200">Returns the requested report.</response>
        /// <response code="400">If the ID is invalid.</response>
        /// <response code="401">If the user is not authenticated or not authorized to access this report.</response>
        /// <response code="404">If the report with the specified ID is not found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Report), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetReportById(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid report ID");
            }

            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            try
            {
                _logger.LogInformation("Retrieving report {ReportId} for user {UserId}", id, userId);
                var result = await _reportService.GetReportByIdAsync(id);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to retrieve report: {Message}", result.Message);
                    return NotFound($"Report with ID {id} not found");
                }

                // Ensure the report belongs to the current user for security
                if (result.Data.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to access report {ReportId} belonging to user {OwnerId}", 
                        userId, id, result.Data.UserId);
                    return Unauthorized("You are not authorized to access this report");
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report {ReportId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while retrieving the report");
            }
        }

        /// <summary>
        /// Retrieves activity reports within a specific date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A collection of activity reports within the specified date range.</returns>
        /// <response code="200">Returns the reports within the date range.</response>
        /// <response code="400">If the date range is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("range")]
        [ProducesResponseType(typeof(IEnumerable<Report>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetReportsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest("Start date must be before or equal to end date");
            }

            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            try
            {
                _logger.LogInformation("Retrieving reports for user {UserId} from {StartDate} to {EndDate}", 
                    userId, startDate, endDate);
                
                var result = await _reportService.GetReportsByDateRangeAsync(startDate, endDate);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to retrieve reports by date range: {Message}", result.Message);
                    return StatusCode(500, result.Message);
                }

                // Filter reports to only include those belonging to the current user
                var userReports = result.Data.Where(r => r.UserId == userId).ToList();
                
                return Ok(userReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports by date range for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving reports");
            }
        }

        /// <summary>
        /// Updates an existing activity report.
        /// </summary>
        /// <param name="id">The ID of the report to update.</param>
        /// <param name="request">The updated report data.</param>
        /// <returns>A response indicating success or failure.</returns>
        /// <response code="200">If the report was successfully updated.</response>
        /// <response code="400">If the ID is invalid or the request data is invalid.</response>
        /// <response code="401">If the user is not authenticated or not authorized to update this report.</response>
        /// <response code="404">If the report with the specified ID is not found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateReport(int id, ReportRequest request)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid report ID");
            }

            if (request == null)
            {
                return BadRequest("Report request cannot be null");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Report text cannot be empty");
            }

            if (request.Text.Length > 500)
            {
                return BadRequest("Report text exceeds maximum length of 500 characters");
            }

            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            try
            {
                _logger.LogInformation("Updating report {ReportId} for user {UserId}", id, userId);
                var result = await _reportService.UpdateReportAsync(id, request, userId);

                if (!result.Succeeded)
                {
                    if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return NotFound($"Report with ID {id} not found");
                    }
                    
                    if (result.Message?.Contains("not authorized", StringComparison.OrdinalIgnoreCase) == true ||
                        result.Message?.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return Unauthorized(result.Message);
                    }
                    
                    _logger.LogWarning("Failed to update report: {Message}", result.Message);
                    return StatusCode(500, result.Message);
                }

                return Ok("Report updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report {ReportId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while updating the report");
            }
        }

        /// <summary>
        /// Deletes a specific activity report.
        /// </summary>
        /// <param name="id">The ID of the report to delete.</param>
        /// <returns>A response indicating success or failure.</returns>
        /// <response code="200">If the report was successfully deleted.</response>
        /// <response code="400">If the ID is invalid.</response>
        /// <response code="401">If the user is not authenticated or not authorized to delete this report.</response>
        /// <response code="404">If the report with the specified ID is not found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteReport(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid report ID");
            }

            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User identity not found");
            }

            try
            {
                _logger.LogInformation("Deleting report {ReportId} for user {UserId}", id, userId);
                var result = await _reportService.DeleteReportAsync(id, userId);

                if (!result.Succeeded)
                {
                    if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return NotFound($"Report with ID {id} not found");
                    }
                    
                    if (result.Message?.Contains("not authorized", StringComparison.OrdinalIgnoreCase) == true ||
                        result.Message?.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return Unauthorized(result.Message);
                    }
                    
                    _logger.LogWarning("Failed to delete report: {Message}", result.Message);
                    return StatusCode(500, result.Message);
                }

                return Ok("Report deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while deleting the report");
            }
        }
    }
}