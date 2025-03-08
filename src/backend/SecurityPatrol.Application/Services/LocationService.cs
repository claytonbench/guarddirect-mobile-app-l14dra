using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation; // v11.0.0
using SecurityPatrol.Application.Validators;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implementation of the ILocationService interface that provides business logic for processing, retrieving, and 
    /// managing location data in the Security Patrol application.
    /// </summary>
    public class LocationService : ILocationService
    {
        private readonly ILocationRecordRepository _locationRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly LocationBatchRequestValidator _validator;

        /// <summary>
        /// Initializes a new instance of the LocationService class with required dependencies.
        /// </summary>
        /// <param name="locationRepository">Repository for accessing location data.</param>
        /// <param name="currentUserService">Service for accessing current user information.</param>
        /// <param name="dateTime">Service for accessing date and time information.</param>
        public LocationService(
            ILocationRecordRepository locationRepository,
            ICurrentUserService currentUserService,
            IDateTime dateTime)
        {
            _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
            _validator = new LocationBatchRequestValidator();
        }

        /// <summary>
        /// Processes a batch of location data points received from a mobile client.
        /// </summary>
        /// <param name="request">The batch request containing user ID and location data points.</param>
        /// <returns>A response indicating which location records were successfully processed and which ones failed.</returns>
        public async Task<LocationSyncResponse> ProcessLocationBatchAsync(LocationBatchRequest request)
        {
            // Validate request using FluentValidation
            await _validator.ValidateAndThrowAsync(request);

            // Create response object
            var response = new LocationSyncResponse();

            // Create a list of location records from the request
            var records = new List<LocationRecord>();
            foreach (var location in request.Locations)
            {
                var record = location.ToEntity();
                record.UserId = request.UserId;
                record.IsSynced = true; // Records coming directly from API are considered synced
                record.Timestamp = location.Timestamp != default ? location.Timestamp : _dateTime.UtcNow();
                record.CreatedBy = request.UserId;
                record.Created = _dateTime.UtcNow();
                records.Add(record);
            }

            // Add location records to the repository
            var savedIds = await _locationRepository.AddRangeAsync(records);
            
            // Update response with successfully saved IDs
            response.SyncedIds = savedIds;
            
            return response;
        }

        /// <summary>
        /// Retrieves location history for a specific user within a time range.
        /// </summary>
        /// <param name="userId">The ID of the user whose location history is being requested.</param>
        /// <param name="startTime">The start of the time range (inclusive).</param>
        /// <param name="endTime">The end of the time range (inclusive).</param>
        /// <returns>A collection of location data points for the specified user within the time range.</returns>
        public async Task<IEnumerable<LocationModel>> GetLocationHistoryAsync(string userId, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (startTime > endTime)
            {
                throw new ArgumentException("Start time must be earlier than end time.");
            }

            var records = await _locationRepository.GetByUserIdAndTimeRangeAsync(userId, startTime, endTime);
            return records.Select(LocationModel.FromEntity);
        }

        /// <summary>
        /// Retrieves the most recent location for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose location is being requested.</param>
        /// <returns>The most recent location data point for the specified user, or null if none exists.</returns>
        public async Task<LocationModel> GetLatestLocationAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            var record = await _locationRepository.GetLatestLocationAsync(userId);
            return LocationModel.FromEntity(record);
        }

        /// <summary>
        /// Retrieves a specified number of recent locations for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose locations are being requested.</param>
        /// <param name="limit">The maximum number of location records to retrieve.</param>
        /// <returns>A collection of recent location data points for the specified user.</returns>
        public async Task<IEnumerable<LocationModel>> GetLocationsByUserIdAsync(string userId, int limit)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (limit <= 0)
            {
                throw new ArgumentException("Limit must be greater than zero.", nameof(limit));
            }

            var records = await _locationRepository.GetByUserIdAsync(userId, limit);
            return records.Select(LocationModel.FromEntity);
        }

        /// <summary>
        /// Deletes location data older than a specified date.
        /// Implements the data retention policy (30 days as specified in requirements).
        /// </summary>
        /// <param name="olderThan">The cutoff date - records older than this will be deleted.</param>
        /// <param name="onlySynced">If true, only deletes records that have been synchronized.</param>
        /// <returns>The number of location records deleted.</returns>
        public async Task<int> CleanupLocationDataAsync(DateTime olderThan, bool onlySynced = true)
        {
            if (olderThan == default)
            {
                throw new ArgumentException("Invalid date specified.", nameof(olderThan));
            }

            return await _locationRepository.DeleteOlderThanAsync(olderThan, onlySynced);
        }

        /// <summary>
        /// Synchronizes pending location records with external systems.
        /// Supports the batch processing approach defined in the requirements.
        /// </summary>
        /// <param name="batchSize">The maximum number of records to process in a single batch.</param>
        /// <returns>A response indicating which location records were successfully synchronized and which ones failed.</returns>
        public async Task<LocationSyncResponse> SyncPendingLocationsAsync(int batchSize = 50)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
            }

            var response = new LocationSyncResponse();
            
            // Get unsynced records up to the specified batch size
            var unsyncedRecords = await _locationRepository.GetUnsyncedRecordsAsync(batchSize);
            
            if (!unsyncedRecords.Any())
            {
                return response; // Return empty response if no unsynced records found
            }

            // In a real implementation, this would involve calls to external APIs
            // For this implementation, we'll assume all records are successfully synced
            
            var syncedIds = new List<int>();
            var failedIds = new List<int>();
            
            foreach (var record in unsyncedRecords)
            {
                try 
                {
                    // In a real implementation, we would make external API calls here
                    // For simplicity, we'll assume all records are successfully synced
                    syncedIds.Add(record.Id);
                    
                    // In a real implementation, we would update the RemoteId field with the ID
                    // returned from the external API
                    // record.RemoteId = externalApiResponse.Id;
                }
                catch (Exception)
                {
                    // Log the exception and add the ID to the failed list
                    failedIds.Add(record.Id);
                }
            }
            
            // Update sync status for the successfully synced records
            if (syncedIds.Any())
            {
                await _locationRepository.UpdateSyncStatusAsync(syncedIds, true);
            }
            
            // Update the response
            response.SyncedIds = syncedIds;
            response.FailedIds = failedIds;
            
            return response;
        }
    }
}