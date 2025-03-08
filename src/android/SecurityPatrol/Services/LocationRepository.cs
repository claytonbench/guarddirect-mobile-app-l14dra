using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Version 8.0+
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SQLite; // SQLite-net-pcl 1.8+

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the ILocationRepository interface that provides data access operations for location records
    /// in the Security Patrol application. This class handles storing, retrieving, and managing GPS location data
    /// in the local SQLite database, supporting both real-time tracking and offline operation with eventual synchronization.
    /// </summary>
    public class LocationRepository : ILocationRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly ILogger<LocationRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the LocationRepository class with the specified dependencies.
        /// </summary>
        /// <param name="databaseService">The database service for accessing the SQLite database.</param>
        /// <param name="authStateProvider">The authentication state provider for obtaining the current user ID.</param>
        /// <param name="logger">The logger for recording repository operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the dependencies are null.</exception>
        public LocationRepository(
            IDatabaseService databaseService,
            IAuthenticationStateProvider authStateProvider,
            ILogger<LocationRepository> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Saves a single location record to the database.
        /// </summary>
        /// <param name="location">The location data to save.</param>
        /// <returns>A task that returns the ID of the saved location record.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the location parameter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if unable to get the current user ID.</exception>
        public async Task<int> SaveLocationAsync(LocationModel location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            _logger.LogDebug("Saving location: Lat: {Latitude}, Long: {Longitude}, Accuracy: {Accuracy}", 
                location.Latitude, location.Longitude, location.Accuracy);

            try
            {
                string userId = await GetCurrentUserIdAsync();
                
                var entity = location.ToEntity();
                entity.UserId = userId;
                
                var connection = await _databaseService.GetConnectionAsync();
                int id = await connection.InsertAsync(entity);
                
                _logger.LogDebug("Successfully saved location with ID: {Id}", id);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving location: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Saves a batch of location records to the database in a single transaction.
        /// Used for efficient batch processing of location updates collected during tracking.
        /// </summary>
        /// <param name="locations">The collection of location data to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the locations parameter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if unable to get the current user ID.</exception>
        public async Task SaveLocationBatchAsync(IEnumerable<LocationModel> locations)
        {
            if (locations == null)
                throw new ArgumentNullException(nameof(locations));

            var locationsList = locations.ToList();
            if (!locationsList.Any())
                return;

            _logger.LogDebug("Saving batch of {Count} locations", locationsList.Count);

            try
            {
                string userId = await GetCurrentUserIdAsync();

                await _databaseService.RunInTransactionAsync(async () =>
                {
                    var connection = await _databaseService.GetConnectionAsync();
                    var entities = locationsList.Select(l => 
                    {
                        var entity = l.ToEntity();
                        entity.UserId = userId;
                        return entity;
                    }).ToList();

                    await connection.InsertAllAsync(entities);
                });

                _logger.LogDebug("Successfully saved batch of {Count} locations", locationsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving location batch: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets a location record by its ID.
        /// </summary>
        /// <param name="id">The ID of the location record to retrieve.</param>
        /// <returns>A task that returns the location record with the specified ID, or null if not found.</returns>
        public async Task<LocationModel> GetLocationAsync(int id)
        {
            _logger.LogDebug("Getting location by ID: {Id}", id);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entity = await connection.GetAsync<LocationRecordEntity>(id);
                
                var model = LocationModel.FromEntity(entity);
                _logger.LogDebug("Retrieved location with ID: {Id}", id);
                
                return model;
            }
            catch (SQLiteException ex) when (ex.Message.Contains("not found"))
            {
                _logger.LogWarning("Location with ID {Id} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location by ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets all location records that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to filter location records.</param>
        /// <returns>A task that returns a collection of location records matching the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the predicate parameter is null.</exception>
        public async Task<IEnumerable<LocationModel>> GetLocationsAsync(Expression<Func<LocationRecordEntity, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _logger.LogDebug("Getting locations by predicate");

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entities = await connection.Table<LocationRecordEntity>()
                    .Where(predicate)
                    .ToListAsync();

                var models = entities.Select(LocationModel.FromEntity).ToList();
                _logger.LogDebug("Retrieved {Count} locations", models.Count);
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations by predicate: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the most recent location records up to the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of the most recent location records.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if limit is less than or equal to zero.</exception>
        public async Task<IEnumerable<LocationModel>> GetRecentLocationsAsync(int limit)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero");

            _logger.LogDebug("Getting {Limit} most recent locations", limit);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entities = await connection.Table<LocationRecordEntity>()
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToListAsync();

                var models = entities.Select(LocationModel.FromEntity).ToList();
                _logger.LogDebug("Retrieved {Count} recent locations", models.Count);
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent locations: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets location records that have not been synchronized with the backend.
        /// Used by the synchronization service to identify records that need to be uploaded.
        /// </summary>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A task that returns a collection of unsynchronized location records.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if limit is less than or equal to zero.</exception>
        public async Task<IEnumerable<LocationModel>> GetPendingSyncLocationsAsync(int limit)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero");

            _logger.LogDebug("Getting up to {Limit} pending sync locations", limit);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entities = await connection.Table<LocationRecordEntity>()
                    .Where(l => !l.IsSynced)
                    .Take(limit)
                    .ToListAsync();

                var models = entities.Select(LocationModel.FromEntity).ToList();
                _logger.LogDebug("Retrieved {Count} pending sync locations", models.Count);
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending sync locations: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates the synchronization status of location records.
        /// Called after successful synchronization with the backend.
        /// </summary>
        /// <param name="ids">The IDs of the location records to update.</param>
        /// <param name="isSynced">The synchronization status to set.</param>
        /// <returns>A task that returns the number of records updated.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the ids parameter is null.</exception>
        public async Task<int> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idsList = ids.ToList();
            if (!idsList.Any())
                return 0;

            _logger.LogDebug("Updating sync status to {IsSynced} for {Count} locations", isSynced, idsList.Count);

            try
            {
                int totalUpdated = 0;
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    var connection = await _databaseService.GetConnectionAsync();
                    foreach (var id in idsList)
                    {
                        var count = await connection.ExecuteAsync(
                            "UPDATE LocationRecord SET IsSynced = ? WHERE Id = ?",
                            isSynced ? 1 : 0, id);
                        totalUpdated += count;
                    }
                });

                _logger.LogDebug("Updated sync status for {Count} of {Total} locations", totalUpdated, idsList.Count);
                return totalUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sync status: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates the remote ID of a location record after successful synchronization.
        /// </summary>
        /// <param name="id">The ID of the location record to update.</param>
        /// <param name="remoteId">The remote ID assigned by the backend.</param>
        /// <returns>A task that returns 1 if the update was successful, 0 otherwise.</returns>
        public async Task<int> UpdateRemoteIdAsync(int id, string remoteId)
        {
            _logger.LogDebug("Updating remote ID to {RemoteId} for location {Id}", remoteId, id);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var count = await connection.ExecuteAsync(
                    "UPDATE LocationRecord SET RemoteId = ? WHERE Id = ?",
                    remoteId, id);

                _logger.LogDebug("Updated remote ID for location {Id}: {Success}", id, count > 0);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating remote ID for location {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Deletes location records older than the specified date that have been synchronized.
        /// Implements the 30-day retention policy for location data.
        /// </summary>
        /// <param name="olderThan">The cutoff date for deletion.</param>
        /// <returns>A task that returns the number of records deleted.</returns>
        public async Task<int> DeleteOldLocationsAsync(DateTime olderThan)
        {
            _logger.LogDebug("Deleting synchronized locations older than {Date}", olderThan);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var count = await connection.ExecuteAsync(
                    "DELETE FROM LocationRecord WHERE Timestamp < ? AND IsSynced = 1",
                    olderThan.ToString("O"));

                _logger.LogDebug("Deleted {Count} old locations", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old locations: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the count of location records that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to filter location records.</param>
        /// <returns>A task that returns the count of matching records.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the predicate parameter is null.</exception>
        public async Task<int> GetLocationCountAsync(Expression<Func<LocationRecordEntity, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _logger.LogDebug("Getting location count by predicate");

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var count = await connection.Table<LocationRecordEntity>()
                    .Where(predicate)
                    .CountAsync();

                _logger.LogDebug("Location count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location count: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets location records within the specified time range.
        /// Useful for generating reports or visualizing patrol routes for a specific period.
        /// </summary>
        /// <param name="startTime">The start of the time range.</param>
        /// <param name="endTime">The end of the time range.</param>
        /// <returns>A task that returns a collection of location records within the time range.</returns>
        public async Task<IEnumerable<LocationModel>> GetLocationsByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            _logger.LogDebug("Getting locations between {StartTime} and {EndTime}", startTime, endTime);

            try
            {
                var connection = await _databaseService.GetConnectionAsync();
                var entities = await connection.Table<LocationRecordEntity>()
                    .Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                    .OrderBy(l => l.Timestamp)
                    .ToListAsync();

                var models = entities.Select(LocationModel.FromEntity).ToList();
                _logger.LogDebug("Retrieved {Count} locations in time range", models.Count);
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations by time range: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the current user ID from the authentication state.
        /// </summary>
        /// <returns>A task that returns the current user ID (phone number).</returns>
        /// <exception cref="InvalidOperationException">Thrown if the user is not authenticated.</exception>
        private async Task<string> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetCurrentState();
            if (!authState.IsAuthenticated || string.IsNullOrEmpty(authState.PhoneNumber))
            {
                throw new InvalidOperationException("User is not authenticated");
            }
            
            return authState.PhoneNumber;
        }
    }
}