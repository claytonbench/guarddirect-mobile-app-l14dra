using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Configuration options for data retention policies
    /// </summary>
    public class DataRetentionOptions
    {
        /// <summary>
        /// Gets or sets the number of days to retain location records
        /// </summary>
        public int LocationRecordRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets the number of days to retain time records
        /// </summary>
        public int TimeRecordRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets the number of days to retain photos
        /// </summary>
        public int PhotoRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets the number of days to retain activity reports
        /// </summary>
        public int ReportRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets the number of days to retain checkpoint verifications
        /// </summary>
        public int CheckpointVerificationRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to only delete records that have been synced to the backend
        /// </summary>
        public bool OnlySyncedRecords { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRetentionOptions"/> class with default values
        /// </summary>
        public DataRetentionOptions()
        {
            // Default values based on requirements
            LocationRecordRetentionDays = 30;
            TimeRecordRetentionDays = 90;
            PhotoRetentionDays = 30;
            ReportRetentionDays = 90;
            CheckpointVerificationRetentionDays = 90;
            OnlySyncedRecords = true;
        }
    }

    /// <summary>
    /// Background job that enforces data retention policies by removing outdated records from the system
    /// </summary>
    public class DataRetentionJob
    {
        private readonly ILocationRecordRepository _locationRecordRepository;
        private readonly ITimeRecordRepository _timeRecordRepository;
        private readonly IPhotoRepository _photoRepository;
        private readonly IReportRepository _reportRepository;
        private readonly ICheckpointVerificationRepository _checkpointVerificationRepository;
        private readonly IDateTime _dateTime;
        private readonly ILogger<DataRetentionJob> _logger;
        private readonly DataRetentionOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRetentionJob"/> class with required dependencies
        /// </summary>
        /// <param name="locationRecordRepository">The location record repository</param>
        /// <param name="timeRecordRepository">The time record repository</param>
        /// <param name="photoRepository">The photo repository</param>
        /// <param name="reportRepository">The report repository</param>
        /// <param name="checkpointVerificationRepository">The checkpoint verification repository</param>
        /// <param name="dateTime">The date/time service</param>
        /// <param name="options">The data retention configuration options</param>
        /// <param name="logger">The logger</param>
        public DataRetentionJob(
            ILocationRecordRepository locationRecordRepository,
            ITimeRecordRepository timeRecordRepository,
            IPhotoRepository photoRepository,
            IReportRepository reportRepository,
            ICheckpointVerificationRepository checkpointVerificationRepository,
            IDateTime dateTime,
            IOptions<DataRetentionOptions> options,
            ILogger<DataRetentionJob> logger)
        {
            _locationRecordRepository = locationRecordRepository ?? throw new ArgumentNullException(nameof(locationRecordRepository));
            _timeRecordRepository = timeRecordRepository ?? throw new ArgumentNullException(nameof(timeRecordRepository));
            _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _checkpointVerificationRepository = checkpointVerificationRepository ?? throw new ArgumentNullException(nameof(checkpointVerificationRepository));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the data retention job, removing outdated records based on configured retention policies
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting data retention job");

            try
            {
                // Calculate cutoff dates for each data type based on retention periods
                var now = _dateTime.UtcNow();
                var locationCutoffDate = now.AddDays(-_options.LocationRecordRetentionDays);
                var timeCutoffDate = now.AddDays(-_options.TimeRecordRetentionDays);
                var photoCutoffDate = now.AddDays(-_options.PhotoRetentionDays);
                var reportCutoffDate = now.AddDays(-_options.ReportRetentionDays);
                var checkpointVerificationCutoffDate = now.AddDays(-_options.CheckpointVerificationRetentionDays);

                // Execute purge operations for each data type
                var locationRecordsDeleted = await PurgeLocationRecordsAsync(locationCutoffDate);
                var timeRecordsDeleted = await PurgeTimeRecordsAsync(timeCutoffDate);
                var photosDeleted = await PurgePhotosAsync(photoCutoffDate);
                var reportsDeleted = await PurgeReportsAsync(reportCutoffDate);
                var checkpointVerificationsDeleted = await PurgeCheckpointVerificationsAsync(checkpointVerificationCutoffDate);

                _logger.LogInformation("Data retention job completed successfully. " +
                    $"Deleted: {locationRecordsDeleted} location records, " +
                    $"{timeRecordsDeleted} time records, " +
                    $"{photosDeleted} photos, " +
                    $"{reportsDeleted} reports, " +
                    $"{checkpointVerificationsDeleted} checkpoint verifications.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing data retention job");
                throw;
            }
        }

        /// <summary>
        /// Removes location records older than the configured retention period
        /// </summary>
        /// <param name="cutoffDate">The cutoff date</param>
        /// <returns>The number of records deleted</returns>
        private async Task<int> PurgeLocationRecordsAsync(DateTime cutoffDate)
        {
            _logger.LogInformation("Purging location records older than {CutoffDate}", cutoffDate);
            var deletedCount = await _locationRecordRepository.DeleteOlderThanAsync(cutoffDate, _options.OnlySyncedRecords);
            _logger.LogInformation("Deleted {Count} location records", deletedCount);
            return deletedCount;
        }

        /// <summary>
        /// Removes time records older than the configured retention period
        /// </summary>
        /// <param name="cutoffDate">The cutoff date</param>
        /// <returns>The number of records deleted</returns>
        private async Task<int> PurgeTimeRecordsAsync(DateTime cutoffDate)
        {
            _logger.LogInformation("Purging time records older than {CutoffDate}", cutoffDate);
            var deletedCount = await _timeRecordRepository.DeleteOlderThanAsync(cutoffDate, _options.OnlySyncedRecords);
            _logger.LogInformation("Deleted {Count} time records", deletedCount);
            return deletedCount;
        }

        /// <summary>
        /// Removes photos older than the configured retention period
        /// </summary>
        /// <param name="cutoffDate">The cutoff date</param>
        /// <returns>The number of photos deleted</returns>
        private async Task<int> PurgePhotosAsync(DateTime cutoffDate)
        {
            _logger.LogInformation("Purging photos older than {CutoffDate}", cutoffDate);
            var deletedCount = await _photoRepository.DeleteOlderThanAsync(cutoffDate);
            _logger.LogInformation("Deleted {Count} photos", deletedCount);
            return deletedCount;
        }

        /// <summary>
        /// Removes reports older than the configured retention period
        /// </summary>
        /// <param name="cutoffDate">The cutoff date</param>
        /// <returns>The number of reports deleted</returns>
        private async Task<int> PurgeReportsAsync(DateTime cutoffDate)
        {
            _logger.LogInformation("Purging reports older than {CutoffDate}", cutoffDate);
            var deletedCount = await _reportRepository.DeleteOlderThanAsync(cutoffDate, _options.OnlySyncedRecords);
            _logger.LogInformation("Deleted {Count} reports", deletedCount);
            return deletedCount;
        }

        /// <summary>
        /// Removes checkpoint verifications older than the configured retention period
        /// </summary>
        /// <param name="cutoffDate">The cutoff date</param>
        /// <returns>The number of checkpoint verifications deleted</returns>
        private async Task<int> PurgeCheckpointVerificationsAsync(DateTime cutoffDate)
        {
            _logger.LogInformation("Purging checkpoint verifications older than {CutoffDate}", cutoffDate);
            var deletedCount = await _checkpointVerificationRepository.DeleteOlderThanAsync(cutoffDate, _options.OnlySyncedRecords);
            _logger.LogInformation("Deleted {Count} checkpoint verifications", deletedCount);
            return deletedCount;
        }
    }
}