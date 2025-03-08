using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implements the ITimeRecordService interface to provide time tracking functionality for security personnel.
    /// </summary>
    public class TimeRecordService : ITimeRecordService
    {
        private readonly ITimeRecordRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<TimeRecordService> _logger;

        /// <summary>
        /// Initializes a new instance of the TimeRecordService class with required dependencies.
        /// </summary>
        /// <param name="repository">Repository for time record data access</param>
        /// <param name="currentUserService">Service to access information about the current user</param>
        /// <param name="dateTime">Service for date/time operations</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public TimeRecordService(
            ITimeRecordRepository repository,
            ICurrentUserService currentUserService,
            IDateTime dateTime,
            ILogger<TimeRecordService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new time record (clock in/out event) based on the provided request.
        /// </summary>
        /// <param name="request">The time record request containing type, timestamp, and location.</param>
        /// <param name="userId">The ID of the user creating the time record.</param>
        /// <returns>Result containing the created time record response or error information.</returns>
        public async Task<Result<TimeRecordResponse>> CreateTimeRecordAsync(TimeRecordRequest request, string userId)
        {
            _logger.LogInformation("Creating new time record for user {UserId}", userId);

            if (request == null)
            {
                return Result.Failure<TimeRecordResponse>("Time record request cannot be null");
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure<TimeRecordResponse>("User ID cannot be null or empty");
            }

            if (string.IsNullOrEmpty(request.Type) || 
                (request.Type.ToLower() != "in" && request.Type.ToLower() != "out"))
            {
                return Result.Failure<TimeRecordResponse>("Time record type must be 'in' or 'out'");
            }

            // Check current status to enforce business rules
            string currentStatus = await _repository.GetCurrentStatusAsync(userId);
            
            if (request.Type.ToLower() == "in" && currentStatus == "in")
            {
                return Result.Failure<TimeRecordResponse>("User is already clocked in");
            }
            
            if (request.Type.ToLower() == "out" && currentStatus == "out")
            {
                return Result.Failure<TimeRecordResponse>("User is already clocked out");
            }

            // Create the time record entity
            var timeRecord = new TimeRecord
            {
                UserId = userId,
                Type = request.Type.ToLower(),
                Timestamp = request.Timestamp != default(DateTime) ? request.Timestamp : _dateTime.UtcNow(),
                Latitude = request.Location?.Latitude ?? 0,
                Longitude = request.Location?.Longitude ?? 0,
                IsSynced = false
            };

            // Save to repository
            var result = await _repository.AddAsync(timeRecord);

            var response = new TimeRecordResponse
            {
                Id = result.Id.ToString(),
                Status = "success"
            };

            _logger.LogInformation("Time record created successfully for user {UserId}, Type: {Type}, ID: {RecordId}", 
                userId, request.Type, response.Id);

            return Result.Success(response);
        }

        /// <summary>
        /// Retrieves a time record by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the time record to retrieve.</param>
        /// <returns>Result containing the time record or error information.</returns>
        public async Task<Result<TimeRecord>> GetTimeRecordByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving time record with ID {RecordId}", id);

            if (id <= 0)
            {
                return Result.Failure<TimeRecord>("Invalid time record ID");
            }

            var timeRecord = await _repository.GetByIdAsync(id);

            if (timeRecord == null)
            {
                return Result.Failure<TimeRecord>($"Time record with ID {id} not found");
            }

            _logger.LogInformation("Retrieved time record with ID {RecordId}", id);
            return Result.Success(timeRecord);
        }

        /// <summary>
        /// Retrieves the time record history for a specific user with pagination.
        /// </summary>
        /// <param name="userId">The ID of the user whose time records to retrieve.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>Result containing a paginated list of time records or error information.</returns>
        public async Task<Result<PaginatedList<TimeRecord>>> GetTimeRecordHistoryAsync(string userId, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Retrieving time record history for user {UserId}, Page: {PageNumber}, Size: {PageSize}", 
                userId, pageNumber, pageSize);

            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure<PaginatedList<TimeRecord>>("User ID cannot be null or empty");
            }

            if (pageNumber <= 0)
            {
                return Result.Failure<PaginatedList<TimeRecord>>("Page number must be greater than 0");
            }

            if (pageSize <= 0)
            {
                return Result.Failure<PaginatedList<TimeRecord>>("Page size must be greater than 0");
            }

            var records = await _repository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);

            _logger.LogInformation("Retrieved {RecordCount} time records for user {UserId}", 
                records.Items.Count, userId);

            return Result.Success(records);
        }

        /// <summary>
        /// Retrieves time records for a specific user within a date range.
        /// </summary>
        /// <param name="userId">The ID of the user whose time records to retrieve.</param>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <returns>Result containing the time records within the date range or error information.</returns>
        public async Task<Result<IEnumerable<TimeRecord>>> GetTimeRecordsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Retrieving time records for user {UserId} between {StartDate} and {EndDate}", 
                userId, startDate, endDate);

            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure<IEnumerable<TimeRecord>>("User ID cannot be null or empty");
            }

            if (startDate == default(DateTime))
            {
                return Result.Failure<IEnumerable<TimeRecord>>("Start date must be specified");
            }

            if (endDate == default(DateTime))
            {
                return Result.Failure<IEnumerable<TimeRecord>>("End date must be specified");
            }

            if (startDate > endDate)
            {
                return Result.Failure<IEnumerable<TimeRecord>>("Start date must be before or equal to end date");
            }

            var records = await _repository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);

            _logger.LogInformation("Retrieved {RecordCount} time records for user {UserId} between {StartDate} and {EndDate}", 
                records.Count(), userId, startDate, endDate);

            return Result.Success(records);
        }

        /// <summary>
        /// Determines if a user is currently clocked in or out.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <returns>Result containing the current status ("in" or "out") or error information.</returns>
        public async Task<Result<string>> GetCurrentStatusAsync(string userId)
        {
            _logger.LogInformation("Checking current clock status for user {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure<string>("User ID cannot be null or empty");
            }

            var status = await _repository.GetCurrentStatusAsync(userId);

            _logger.LogInformation("Current clock status for user {UserId} is {Status}", userId, status);

            return Result.Success(status);
        }

        /// <summary>
        /// Retrieves the most recent time record for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose latest time record to retrieve.</param>
        /// <returns>Result containing the most recent time record or error information.</returns>
        public async Task<Result<TimeRecord>> GetLatestTimeRecordAsync(string userId)
        {
            _logger.LogInformation("Retrieving latest time record for user {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure<TimeRecord>("User ID cannot be null or empty");
            }

            var record = await _repository.GetLatestByUserIdAsync(userId);

            if (record != null)
            {
                _logger.LogInformation("Retrieved latest time record for user {UserId}, Type: {Type}, Timestamp: {Timestamp}", 
                    userId, record.Type, record.Timestamp);
            }
            else
            {
                _logger.LogInformation("No time records found for user {UserId}", userId);
            }

            return Result.Success(record);
        }

        /// <summary>
        /// Deletes a time record by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the time record to delete.</param>
        /// <param name="userId">The ID of the user requesting the deletion.</param>
        /// <returns>Result indicating success or failure of the deletion operation.</returns>
        public async Task<Result> DeleteTimeRecordAsync(int id, string userId)
        {
            _logger.LogInformation("Deleting time record with ID {RecordId} for user {UserId}", id, userId);

            if (id <= 0)
            {
                return Result.Failure("Invalid time record ID");
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure("User ID cannot be null or empty");
            }

            var timeRecord = await _repository.GetByIdAsync(id);

            if (timeRecord == null)
            {
                return Result.Failure($"Time record with ID {id} not found");
            }

            // Ensure the record belongs to the user
            if (timeRecord.UserId != userId)
            {
                _logger.LogWarning("Unauthorized attempt to delete time record {RecordId} by user {UserId}", id, userId);
                return Result.Failure("You are not authorized to delete this time record");
            }

            await _repository.DeleteAsync(id);

            _logger.LogInformation("Time record with ID {RecordId} deleted successfully", id);

            return Result.Success();
        }

        /// <summary>
        /// Deletes time records older than a specified date.
        /// </summary>
        /// <param name="olderThan">The cutoff date for deletion.</param>
        /// <param name="onlySynced">If true, only deletes records that have been synced.</param>
        /// <returns>Result containing the number of records deleted or error information.</returns>
        public async Task<Result<int>> CleanupOldRecordsAsync(DateTime olderThan, bool onlySynced)
        {
            _logger.LogInformation("Cleaning up time records older than {Date}, OnlySynced: {OnlySynced}", 
                olderThan, onlySynced);

            if (olderThan == default(DateTime))
            {
                return Result.Failure<int>("Cutoff date must be specified");
            }

            int deleted = await _repository.DeleteOlderThanAsync(olderThan, onlySynced);

            _logger.LogInformation("Deleted {DeletedCount} old time records", deleted);

            return Result.Success(deleted);
        }

        /// <summary>
        /// Updates the sync status of a time record.
        /// </summary>
        /// <param name="id">The ID of the time record to update.</param>
        /// <param name="isSynced">The new sync status.</param>
        /// <returns>Result indicating success or failure of the update operation.</returns>
        public async Task<Result> UpdateSyncStatusAsync(int id, bool isSynced)
        {
            _logger.LogInformation("Updating sync status for time record {RecordId} to {IsSynced}", id, isSynced);

            if (id <= 0)
            {
                return Result.Failure("Invalid time record ID");
            }

            await _repository.UpdateSyncStatusAsync(id, isSynced);

            _logger.LogInformation("Sync status updated successfully for time record {RecordId}", id);

            return Result.Success();
        }

        /// <summary>
        /// Retrieves time records that have not been synced with mobile clients.
        /// </summary>
        /// <returns>Result containing the unsynced time records or error information.</returns>
        public async Task<Result<IEnumerable<TimeRecord>>> GetUnsyncedRecordsAsync()
        {
            _logger.LogInformation("Retrieving unsynced time records");

            var records = await _repository.GetUnsyncedAsync();

            _logger.LogInformation("Retrieved {RecordCount} unsynced time records", records.Count());

            return Result.Success(records);
        }
    }
}