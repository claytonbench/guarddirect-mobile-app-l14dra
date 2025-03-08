using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging; // Version 8.0.0
using CommunityToolkit.Mvvm.Input; // Version Latest
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the checkpoint verification screen that handles the verification of security patrol checkpoints.
    /// It manages the checkpoint verification process, including location permission checks, proximity detection,
    /// and verification submission.
    /// </summary>
    public class VerificationViewModel : BaseViewModel
    {
        private readonly IPatrolService _patrolService;
        private readonly ILocationService _locationService;
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<VerificationViewModel> _logger;
        
        private int _checkpointId;
        private int _locationId;
        private CheckpointModel _checkpoint;
        private double _currentDistance = -1;
        private bool _isInRange;
        private bool _hasLocationPermission;
        private bool _isVerified;

        /// <summary>
        /// Gets or sets the ID of the checkpoint being verified.
        /// </summary>
        public int CheckpointId 
        { 
            get => _checkpointId; 
            set => SetProperty(ref _checkpointId, value); 
        }

        /// <summary>
        /// Gets or sets the ID of the patrol location.
        /// </summary>
        public int LocationId 
        { 
            get => _locationId; 
            set => SetProperty(ref _locationId, value); 
        }

        /// <summary>
        /// Gets or sets the checkpoint model being verified.
        /// </summary>
        public CheckpointModel Checkpoint 
        { 
            get => _checkpoint; 
            private set => SetProperty(ref _checkpoint, value); 
        }

        /// <summary>
        /// Gets or sets the current distance to the checkpoint in feet.
        /// Value of -1 indicates distance is unknown.
        /// </summary>
        public double CurrentDistance 
        { 
            get => _currentDistance; 
            private set => SetProperty(ref _currentDistance, value); 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user is within range of the checkpoint.
        /// </summary>
        public bool IsInRange 
        { 
            get => _isInRange; 
            private set => SetProperty(ref _isInRange, value); 
        }

        /// <summary>
        /// Gets or sets a value indicating whether location permissions are granted.
        /// </summary>
        public bool HasLocationPermission 
        { 
            get => _hasLocationPermission; 
            private set => SetProperty(ref _hasLocationPermission, value); 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the checkpoint has been verified.
        /// </summary>
        public bool IsVerified 
        { 
            get => _isVerified; 
            private set => SetProperty(ref _isVerified, value); 
        }

        /// <summary>
        /// Gets the command to verify the checkpoint.
        /// </summary>
        public ICommand VerifyCheckpointCommand { get; }

        /// <summary>
        /// Gets the command to request location permissions.
        /// </summary>
        public ICommand RequestLocationPermissionCommand { get; }

        /// <summary>
        /// Gets the command to return to the patrol screen.
        /// </summary>
        public ICommand ReturnToPatrolCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationViewModel"/> class with required services.
        /// </summary>
        /// <param name="navigationService">Service used for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service used to access the authentication state.</param>
        /// <param name="patrolService">Service for managing patrol operations and checkpoints.</param>
        /// <param name="locationService">Service for accessing device location.</param>
        /// <param name="telemetryService">Service for tracking application telemetry and logging.</param>
        /// <param name="logger">Logger for recording operation details.</param>
        public VerificationViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IPatrolService patrolService,
            ILocationService locationService,
            ITelemetryService telemetryService,
            ILogger<VerificationViewModel> logger)
            : base(navigationService, authenticationStateProvider)
        {
            _patrolService = patrolService ?? throw new ArgumentNullException(nameof(patrolService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Title = "Checkpoint Verification";
            
            // Initialize default values
            CurrentDistance = -1;
            IsInRange = false;
            HasLocationPermission = false;
            IsVerified = false;

            // Initialize commands
            VerifyCheckpointCommand = new AsyncRelayCommand(VerifyCheckpointAsync, 
                () => HasLocationPermission && IsInRange && !IsVerified);
            RequestLocationPermissionCommand = new AsyncRelayCommand(RequestLocationPermissionAsync);
            ReturnToPatrolCommand = new AsyncRelayCommand(ReturnToPatrolAsync);

            // Subscribe to events
            _patrolService.CheckpointProximityChanged += HandleCheckpointProximityChanged;
            _locationService.LocationChanged += HandleLocationChanged;
        }

        /// <summary>
        /// Initializes the ViewModel when navigated to, checking permissions and loading checkpoint data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing VerificationViewModel for checkpoint {CheckpointId}", CheckpointId);
            
            // Check location permissions
            bool hasPermission = await PermissionHelper.CheckLocationPermissionsAsync(_logger);
            HasLocationPermission = hasPermission;
            
            if (hasPermission)
            {
                await LoadCheckpointDataAsync();
            }
            else
            {
                SetError(ErrorMessages.LocationPermissionDenied);
            }
            
            _telemetryService.TrackPageView("CheckpointVerification");
            _logger.LogInformation("VerificationViewModel initialized");
        }

        /// <summary>
        /// Called when the page using this ViewModel is navigated to, extracting navigation parameters.
        /// </summary>
        /// <param name="parameters">Navigation parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedTo(Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                if (parameters.TryGetValue(NavigationConstants.ParamCheckpointId, out var checkpointIdObj) &&
                    checkpointIdObj is int checkpointId)
                {
                    CheckpointId = checkpointId;
                }
                
                if (parameters.TryGetValue(NavigationConstants.ParamLocationId, out var locationIdObj) &&
                    locationIdObj is int locationId)
                {
                    LocationId = locationId;
                }
            }
            
            await base.OnNavigatedTo(parameters);
            
            _logger.LogInformation("Navigated to checkpoint verification. CheckpointId: {CheckpointId}, LocationId: {LocationId}", 
                CheckpointId, LocationId);
        }

        /// <summary>
        /// Called when navigating away from the page using this ViewModel, cleaning up resources.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedFrom()
        {
            // Unsubscribe from events to prevent memory leaks
            _patrolService.CheckpointProximityChanged -= HandleCheckpointProximityChanged;
            _locationService.LocationChanged -= HandleLocationChanged;
            
            await base.OnNavigatedFrom();
        }

        /// <summary>
        /// Loads the checkpoint data for the specified checkpoint ID.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LoadCheckpointDataAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                try
                {
                    // Get all checkpoints for the location
                    var checkpoints = await _patrolService.GetCheckpoints(LocationId);
                    
                    // Find the specific checkpoint
                    var checkpoint = checkpoints.FirstOrDefault(c => c.Id == CheckpointId);
                    
                    if (checkpoint != null)
                    {
                        Checkpoint = checkpoint;
                        IsVerified = checkpoint.IsVerified;
                        
                        // Update proximity information
                        await UpdateProximityAsync();
                    }
                    else
                    {
                        _logger.LogError("Checkpoint not found. CheckpointId: {CheckpointId}, LocationId: {LocationId}", 
                            CheckpointId, LocationId);
                        SetError("Checkpoint not found. Please return to patrol screen.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading checkpoint data");
                    throw; // Let ExecuteWithBusyIndicator handle the error
                }
            });
        }

        /// <summary>
        /// Updates the proximity information based on the current location.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateProximityAsync()
        {
            if (Checkpoint == null)
                return;
                
            try
            {
                // Get current location
                var location = await _locationService.GetCurrentLocation();
                
                if (location != null)
                {
                    // Calculate distance to checkpoint
                    double distanceMeters = Checkpoint.CalculateDistance(location.Latitude, location.Longitude);
                    double distanceFeet = LocationHelper.ConvertMetersToFeet(distanceMeters);
                    
                    // Update properties
                    CurrentDistance = Math.Round(distanceFeet, 1);
                    IsInRange = distanceFeet <= _patrolService.ProximityThresholdFeet;
                    
                    _logger.LogDebug("Distance to checkpoint: {Distance} feet, IsInRange: {IsInRange}", 
                        CurrentDistance, IsInRange);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating proximity information");
                SetError("Error determining proximity to checkpoint: " + ex.Message);
            }
        }

        /// <summary>
        /// Verifies the checkpoint if the user is within the required proximity.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task VerifyCheckpointAsync()
        {
            ClearError();
            
            if (IsVerified)
            {
                SetError(ErrorMessages.CheckpointAlreadyVerified);
                return;
            }
            
            if (!IsInRange)
            {
                SetError(ErrorMessages.CheckpointTooFar);
                return;
            }
            
            await ExecuteWithBusyIndicator(async () =>
            {
                bool success = await _patrolService.VerifyCheckpoint(CheckpointId);
                
                if (success)
                {
                    IsVerified = true;
                    await DialogHelper.DisplaySuccessAsync("Checkpoint verified successfully!");
                    
                    _telemetryService.TrackEvent("CheckpointVerified", new Dictionary<string, string>
                    {
                        { "CheckpointId", CheckpointId.ToString() },
                        { "LocationId", LocationId.ToString() }
                    });
                }
                else
                {
                    await DialogHelper.DisplayErrorAsync(ErrorMessages.CheckpointVerificationFailed);
                    
                    _telemetryService.TrackEvent("CheckpointVerificationFailed", new Dictionary<string, string>
                    {
                        { "CheckpointId", CheckpointId.ToString() },
                        { "LocationId", LocationId.ToString() }
                    });
                }
            });
        }

        /// <summary>
        /// Requests location permission from the user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RequestLocationPermissionAsync()
        {
            ClearError();
            
            bool permissionGranted = await PermissionHelper.RequestLocationPermissionsAsync(true, _logger);
            HasLocationPermission = permissionGranted;
            
            if (permissionGranted)
            {
                await LoadCheckpointDataAsync();
            }
            else
            {
                SetError(ErrorMessages.LocationPermissionDenied);
            }
            
            _telemetryService.TrackEvent("LocationPermissionRequested", new Dictionary<string, string>
            {
                { "Granted", permissionGranted.ToString() }
            });
        }

        /// <summary>
        /// Navigates back to the patrol page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ReturnToPatrolAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamLocationId, LocationId }
            };
            
            await NavigationService.NavigateToAsync(NavigationConstants.PatrolPage, parameters);
        }

        /// <summary>
        /// Handles the checkpoint proximity changed event from the patrol service.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing proximity information.</param>
        private void HandleCheckpointProximityChanged(object sender, CheckpointProximityEventArgs e)
        {
            if (e.CheckpointId != CheckpointId)
                return;
                
            CurrentDistance = e.Distance;
            IsInRange = e.IsInRange;
            
            _logger.LogDebug("Proximity changed: CheckpointId: {CheckpointId}, Distance: {Distance}, IsInRange: {IsInRange}", 
                e.CheckpointId, e.Distance, e.IsInRange);
        }

        /// <summary>
        /// Handles the location changed event from the location service.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing location information.</param>
        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (Checkpoint == null || e.Location == null)
                return;
                
            // Calculate distance to checkpoint
            double distanceMeters = Checkpoint.CalculateDistance(e.Location.Latitude, e.Location.Longitude);
            double distanceFeet = LocationHelper.ConvertMetersToFeet(distanceMeters);
            
            // Update properties
            CurrentDistance = Math.Round(distanceFeet, 1);
            IsInRange = distanceFeet <= _patrolService.ProximityThresholdFeet;
            
            // Only log significant changes to reduce noise
            if (Math.Abs(CurrentDistance - distanceFeet) > 10)
            {
                _logger.LogDebug("Location changed: Distance to checkpoint: {Distance} feet, IsInRange: {IsInRange}", 
                    CurrentDistance, IsInRange);
            }
        }

        /// <summary>
        /// Disposes of resources used by the ViewModel.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            _patrolService.CheckpointProximityChanged -= HandleCheckpointProximityChanged;
            _locationService.LocationChanged -= HandleLocationChanged;
            
            base.Dispose();
        }
    }
}