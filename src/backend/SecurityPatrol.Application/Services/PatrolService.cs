using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Application.Services
{
    /// <summary>
    /// Implements the IPatrolService interface to provide business logic for patrol management operations
    /// including retrieving patrol locations, managing checkpoints, and processing checkpoint verifications.
    /// </summary>
    public class PatrolService : IPatrolService
    {
        private readonly IPatrolLocationRepository _patrolLocationRepository;
        private readonly ICheckpointRepository _checkpointRepository;
        private readonly ICheckpointVerificationRepository _verificationRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;

        /// <summary>
        /// Initializes a new instance of the PatrolService class with required dependencies
        /// </summary>
        /// <param name="patrolLocationRepository">Repository for patrol location data access</param>
        /// <param name="checkpointRepository">Repository for checkpoint data access</param>
        /// <param name="verificationRepository">Repository for checkpoint verification data access</param>
        /// <param name="currentUserService">Service to access current user information</param>
        /// <param name="dateTime">Service to access system date and time</param>
        public PatrolService(
            IPatrolLocationRepository patrolLocationRepository,
            ICheckpointRepository checkpointRepository,
            ICheckpointVerificationRepository verificationRepository,
            ICurrentUserService currentUserService,
            IDateTime dateTime)
        {
            _patrolLocationRepository = patrolLocationRepository ?? 
                throw new ArgumentNullException(nameof(patrolLocationRepository));
            _checkpointRepository = checkpointRepository ?? 
                throw new ArgumentNullException(nameof(checkpointRepository));
            _verificationRepository = verificationRepository ?? 
                throw new ArgumentNullException(nameof(verificationRepository));
            _currentUserService = currentUserService ?? 
                throw new ArgumentNullException(nameof(currentUserService));
            _dateTime = dateTime ?? 
                throw new ArgumentNullException(nameof(dateTime));
        }

        /// <summary>
        /// Retrieves all patrol locations available in the system
        /// </summary>
        /// <returns>A result containing a collection of patrol locations if successful</returns>
        public async Task<Result<IEnumerable<PatrolLocation>>> GetLocationsAsync()
        {
            var locations = await _patrolLocationRepository.GetAllAsync();
            
            if (locations == null || !locations.Any())
            {
                return Result<IEnumerable<PatrolLocation>>.Failure("No patrol locations found.");
            }
            
            return Result<IEnumerable<PatrolLocation>>.Success(locations);
        }

        /// <summary>
        /// Retrieves a specific patrol location by its ID
        /// </summary>
        /// <param name="locationId">The ID of the patrol location to retrieve</param>
        /// <returns>A result containing the patrol location if found</returns>
        public async Task<Result<PatrolLocation>> GetLocationByIdAsync(int locationId)
        {
            if (locationId <= 0)
            {
                return Result<PatrolLocation>.Failure("Location ID must be greater than zero.");
            }
            
            var location = await _patrolLocationRepository.GetByIdAsync(locationId);
            
            if (location == null)
            {
                return Result<PatrolLocation>.Failure($"Patrol location with ID {locationId} not found.");
            }
            
            return Result<PatrolLocation>.Success(location);
        }

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <returns>A result containing a collection of checkpoint models if successful</returns>
        public async Task<Result<IEnumerable<CheckpointModel>>> GetCheckpointsByLocationIdAsync(int locationId)
        {
            if (locationId <= 0)
            {
                return Result<IEnumerable<CheckpointModel>>.Failure("Location ID must be greater than zero.");
            }
            
            // Verify the location exists
            var locationExists = await _patrolLocationRepository.ExistsAsync(locationId);
            if (!locationExists)
            {
                return Result<IEnumerable<CheckpointModel>>.Failure($"Patrol location with ID {locationId} not found.");
            }
            
            var checkpoints = await _checkpointRepository.GetByLocationIdAsync(locationId);
            
            if (checkpoints == null || !checkpoints.Any())
            {
                return Result<IEnumerable<CheckpointModel>>.Success(new List<CheckpointModel>());
            }
            
            // Get the current user ID to check verification status
            var userId = _currentUserService.UserId;
            IEnumerable<CheckpointVerification> userVerifications = new List<CheckpointVerification>();
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Get the user's verifications for checkpoints in this location
                userVerifications = await _verificationRepository.GetByUserAndLocationIdAsync(userId, locationId);
            }
            
            // Convert to checkpoint models and set verification status
            var checkpointModels = checkpoints.Select(cp => 
            {
                var verification = userVerifications.FirstOrDefault(v => v.CheckpointId == cp.Id);
                return CheckpointModel.FromEntity(
                    cp, 
                    verification != null, 
                    verification?.Timestamp);
            }).ToList();
            
            return Result<IEnumerable<CheckpointModel>>.Success(checkpointModels);
        }

        /// <summary>
        /// Retrieves a specific checkpoint by its ID
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to retrieve</param>
        /// <returns>A result containing the checkpoint model if found</returns>
        public async Task<Result<CheckpointModel>> GetCheckpointByIdAsync(int checkpointId)
        {
            if (checkpointId <= 0)
            {
                return Result<CheckpointModel>.Failure("Checkpoint ID must be greater than zero.");
            }
            
            var checkpoint = await _checkpointRepository.GetByIdAsync(checkpointId);
            
            if (checkpoint == null)
            {
                return Result<CheckpointModel>.Failure($"Checkpoint with ID {checkpointId} not found.");
            }
            
            // Get the current user ID to check verification status
            var userId = _currentUserService.UserId;
            bool isVerified = false;
            DateTime? verificationTime = null;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Check if the user has verified this checkpoint
                var verification = await _verificationRepository.GetByUserAndCheckpointIdAsync(userId, checkpointId);
                isVerified = verification != null;
                verificationTime = verification?.Timestamp;
            }
            
            var checkpointModel = CheckpointModel.FromEntity(checkpoint, isVerified, verificationTime);
            
            return Result<CheckpointModel>.Success(checkpointModel);
        }

        /// <summary>
        /// Processes a checkpoint verification request from a security officer
        /// </summary>
        /// <param name="request">The verification request containing checkpoint and location information</param>
        /// <param name="userId">The ID of the user verifying the checkpoint</param>
        /// <returns>A result containing the verification response if successful</returns>
        public async Task<Result<CheckpointVerificationResponse>> VerifyCheckpointAsync(CheckpointVerificationRequest request, string userId)
        {
            if (request == null || request.CheckpointId <= 0)
            {
                return Result<CheckpointVerificationResponse>.Failure("Invalid checkpoint verification request.");
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                return Result<CheckpointVerificationResponse>.Failure("User ID is required for checkpoint verification.");
            }
            
            // Check if the checkpoint exists
            var checkpoint = await _checkpointRepository.GetByIdAsync(request.CheckpointId);
            if (checkpoint == null)
            {
                return Result<CheckpointVerificationResponse>.Failure($"Checkpoint with ID {request.CheckpointId} not found.");
            }
            
            // Check if the checkpoint is already verified by this user
            var existingVerification = await _verificationRepository.GetByUserAndCheckpointIdAsync(userId, request.CheckpointId);
            if (existingVerification != null)
            {
                var existingResponse = new CheckpointVerificationResponse
                {
                    CheckpointId = existingVerification.CheckpointId,
                    UserId = existingVerification.UserId,
                    Timestamp = existingVerification.Timestamp,
                    Latitude = existingVerification.Latitude,
                    Longitude = existingVerification.Longitude,
                    Status = "Already Verified"
                };
                
                return Result<CheckpointVerificationResponse>.Success(existingResponse);
            }
            
            // Create a new verification
            var verification = new CheckpointVerification
            {
                CheckpointId = request.CheckpointId,
                UserId = userId,
                Timestamp = _dateTime.Now,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsSynced = false,
                CreatedBy = userId,
                Created = _dateTime.Now
            };
            
            // Save the verification
            var addResult = await _verificationRepository.AddAsync(verification);
            if (!addResult)
            {
                return Result<CheckpointVerificationResponse>.Failure("Failed to save checkpoint verification.");
            }
            
            // Get the saved verification (with the ID)
            var savedVerification = await _verificationRepository.GetByUserAndCheckpointIdAsync(userId, request.CheckpointId);
            
            var response = new CheckpointVerificationResponse
            {
                CheckpointId = savedVerification.CheckpointId,
                UserId = savedVerification.UserId,
                Timestamp = savedVerification.Timestamp,
                Latitude = savedVerification.Latitude,
                Longitude = savedVerification.Longitude,
                Status = "Verified"
            };
            
            return Result<CheckpointVerificationResponse>.Success(response);
        }

        /// <summary>
        /// Retrieves the current status of a patrol for a specific location and user
        /// </summary>
        /// <param name="locationId">The ID of the patrol location</param>
        /// <param name="userId">The ID of the user performing the patrol</param>
        /// <returns>A result containing the patrol status model if successful</returns>
        public async Task<Result<PatrolStatusModel>> GetPatrolStatusAsync(int locationId, string userId)
        {
            if (locationId <= 0)
            {
                return Result<PatrolStatusModel>.Failure("Location ID must be greater than zero.");
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                return Result<PatrolStatusModel>.Failure("User ID is required to get patrol status.");
            }
            
            // Check if the location exists
            var locationExists = await _patrolLocationRepository.ExistsAsync(locationId);
            if (!locationExists)
            {
                return Result<PatrolStatusModel>.Failure($"Patrol location with ID {locationId} not found.");
            }
            
            // Get all checkpoints for the location
            var checkpoints = await _checkpointRepository.GetByLocationIdAsync(locationId);
            
            // Get all verifications for the user at this location
            var verifications = await _verificationRepository.GetByUserAndLocationIdAsync(userId, locationId);
            
            // Create the patrol status model
            var patrolStatus = new PatrolStatusModel
            {
                LocationId = locationId,
                TotalCheckpoints = checkpoints.Count(),
                VerifiedCheckpoints = verifications.Count(),
                LastVerificationTime = verifications.Any() ? verifications.Max(v => v.Timestamp) : null,
                IsComplete = checkpoints.Count() > 0 && checkpoints.Count() == verifications.Count()
            };
            
            return Result<PatrolStatusModel>.Success(patrolStatus);
        }

        /// <summary>
        /// Retrieves checkpoints that are within a specified distance of the user's current location
        /// </summary>
        /// <param name="latitude">The latitude of the user's current position</param>
        /// <param name="longitude">The longitude of the user's current position</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of nearby checkpoint models if successful</returns>
        public async Task<Result<IEnumerable<CheckpointModel>>> GetNearbyCheckpointsAsync(double latitude, double longitude, double radiusInMeters)
        {
            if (latitude < -90 || latitude > 90)
            {
                return Result<IEnumerable<CheckpointModel>>.Failure("Latitude must be between -90 and 90 degrees.");
            }
            
            if (longitude < -180 || longitude > 180)
            {
                return Result<IEnumerable<CheckpointModel>>.Failure("Longitude must be between -180 and 180 degrees.");
            }
            
            if (radiusInMeters <= 0)
            {
                return Result<IEnumerable<CheckpointModel>>.Failure("Radius must be greater than zero meters.");
            }
            
            var checkpoints = await _checkpointRepository.GetNearbyCheckpointsAsync(latitude, longitude, radiusInMeters);
            
            if (checkpoints == null || !checkpoints.Any())
            {
                return Result<IEnumerable<CheckpointModel>>.Success(new List<CheckpointModel>());
            }
            
            // Get the current user ID to check verification status
            var userId = _currentUserService.UserId;
            IEnumerable<CheckpointVerification> userVerifications = new List<CheckpointVerification>();
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Get all verifications for the user
                userVerifications = await _verificationRepository.GetByUserIdAsync(userId);
            }
            
            // Convert to checkpoint models and set verification status
            var checkpointModels = checkpoints.Select(cp => 
            {
                var verification = userVerifications.FirstOrDefault(v => v.CheckpointId == cp.Id);
                return CheckpointModel.FromEntity(
                    cp, 
                    verification != null, 
                    verification?.Timestamp);
            }).ToList();
            
            return Result<IEnumerable<CheckpointModel>>.Success(checkpointModels);
        }

        /// <summary>
        /// Retrieves all checkpoint verifications for a specific user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A result containing a collection of checkpoint verifications if successful</returns>
        public async Task<Result<IEnumerable<CheckpointVerification>>> GetUserVerificationsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Result<IEnumerable<CheckpointVerification>>.Failure("User ID is required to get verifications.");
            }
            
            var verifications = await _verificationRepository.GetByUserIdAsync(userId);
            
            if (verifications == null)
            {
                return Result<IEnumerable<CheckpointVerification>>.Success(new List<CheckpointVerification>());
            }
            
            return Result<IEnumerable<CheckpointVerification>>.Success(verifications);
        }

        /// <summary>
        /// Retrieves checkpoint verifications for a specific user within a date range
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <returns>A result containing a collection of checkpoint verifications if successful</returns>
        public async Task<Result<IEnumerable<CheckpointVerification>>> GetUserVerificationsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Result<IEnumerable<CheckpointVerification>>.Failure("User ID is required to get verifications.");
            }
            
            if (startDate == default)
            {
                return Result<IEnumerable<CheckpointVerification>>.Failure("Start date is required.");
            }
            
            if (endDate == default || endDate < startDate)
            {
                return Result<IEnumerable<CheckpointVerification>>.Failure("End date must be valid and greater than or equal to start date.");
            }
            
            var verifications = await _verificationRepository.GetByUserAndDateRangeAsync(userId, startDate, endDate);
            
            if (verifications == null)
            {
                return Result<IEnumerable<CheckpointVerification>>.Success(new List<CheckpointVerification>());
            }
            
            return Result<IEnumerable<CheckpointVerification>>.Success(verifications);
        }

        /// <summary>
        /// Retrieves patrol locations that are within a specified distance of the user's current location
        /// </summary>
        /// <param name="latitude">The latitude of the user's current position</param>
        /// <param name="longitude">The longitude of the user's current position</param>
        /// <param name="radiusInMeters">The search radius in meters</param>
        /// <returns>A result containing a collection of nearby patrol locations if successful</returns>
        public async Task<Result<IEnumerable<PatrolLocation>>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusInMeters)
        {
            if (latitude < -90 || latitude > 90)
            {
                return Result<IEnumerable<PatrolLocation>>.Failure("Latitude must be between -90 and 90 degrees.");
            }
            
            if (longitude < -180 || longitude > 180)
            {
                return Result<IEnumerable<PatrolLocation>>.Failure("Longitude must be between -180 and 180 degrees.");
            }
            
            if (radiusInMeters <= 0)
            {
                return Result<IEnumerable<PatrolLocation>>.Failure("Radius must be greater than zero meters.");
            }
            
            var locations = await _patrolLocationRepository.GetNearbyLocationsAsync(latitude, longitude, radiusInMeters);
            
            if (locations == null || !locations.Any())
            {
                return Result<IEnumerable<PatrolLocation>>.Success(new List<PatrolLocation>());
            }
            
            return Result<IEnumerable<PatrolLocation>>.Success(locations);
        }

        /// <summary>
        /// Checks if a specific checkpoint has been verified by a user
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint</param>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A result containing a boolean indicating if the checkpoint is verified</returns>
        public async Task<Result<bool>> IsCheckpointVerifiedAsync(int checkpointId, string userId)
        {
            if (checkpointId <= 0)
            {
                return Result<bool>.Failure("Checkpoint ID must be greater than zero.");
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                return Result<bool>.Failure("User ID is required to check verification status.");
            }
            
            // Check if the checkpoint exists
            var checkpointExists = await _checkpointRepository.ExistsAsync(checkpointId);
            if (!checkpointExists)
            {
                return Result<bool>.Failure($"Checkpoint with ID {checkpointId} not found.");
            }
            
            // Get all verifications for the user
            var userVerifications = await _verificationRepository.GetByUserIdAsync(userId);
            
            // Check if the checkpoint is verified
            bool isVerified = userVerifications.Any(v => v.CheckpointId == checkpointId);
            
            return Result<bool>.Success(isVerified);
        }

        /// <summary>
        /// Calculates the distance between two geographic coordinates using the Haversine formula
        /// </summary>
        /// <param name="lat1">Latitude of the first point in degrees</param>
        /// <param name="lon1">Longitude of the first point in degrees</param>
        /// <param name="lat2">Latitude of the second point in degrees</param>
        /// <param name="lon2">Longitude of the second point in degrees</param>
        /// <returns>The distance between the coordinates in meters</returns>
        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth radius in meters
            const double earthRadius = 6371000;
            
            // Convert latitude and longitude from degrees to radians
            double lat1Rad = lat1 * Math.PI / 180;
            double lon1Rad = lon1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;
            double lon2Rad = lon2 * Math.PI / 180;
            
            // Calculate differences
            double latDiff = lat2Rad - lat1Rad;
            double lonDiff = lon2Rad - lon1Rad;
            
            // Haversine formula
            double a = Math.Sin(latDiff / 2) * Math.Sin(latDiff / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(lonDiff / 2) * Math.Sin(lonDiff / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;
            
            return distance;
        }
    }
}