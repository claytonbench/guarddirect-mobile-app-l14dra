using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Collections.ObjectModel; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Windows.Input; // Version 8.0.0
using CommunityToolkit.Mvvm.ComponentModel; // Version Latest
using CommunityToolkit.Mvvm.Input; // Version Latest
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the Patrol page that manages patrol operations, checkpoint visualization, and verification.
    /// Handles location selection, checkpoint display, proximity detection, and verification of checkpoints during security patrols.
    /// </summary>
    public class PatrolViewModel : BaseViewModel
    {
        #region Services

        private readonly IPatrolService _patrolService;
        private readonly IMapService _mapService;
        private readonly ILocationService _locationService;

        #endregion

        #region Properties

        private ObservableCollection<LocationModel> _locations;
        /// <summary>
        /// Gets or sets the collection of available patrol locations.
        /// </summary>
        public ObservableCollection<LocationModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        private ObservableCollection<CheckpointModel> _checkpoints;
        /// <summary>
        /// Gets or sets the collection of checkpoints for the selected location.
        /// </summary>
        public ObservableCollection<CheckpointModel> Checkpoints
        {
            get => _checkpoints;
            set => SetProperty(ref _checkpoints, value);
        }

        private LocationModel _selectedLocation;
        /// <summary>
        /// Gets or sets the currently selected patrol location.
        /// </summary>
        public LocationModel SelectedLocation
        {
            get => _selectedLocation;
            set => SetProperty(ref _selectedLocation, value, onChanged: () => IsLocationSelected = value != null);
        }

        private CheckpointModel _selectedCheckpoint;
        /// <summary>
        /// Gets or sets the currently selected checkpoint.
        /// </summary>
        public CheckpointModel SelectedCheckpoint
        {
            get => _selectedCheckpoint;
            set => SetProperty(ref _selectedCheckpoint, value);
        }

        private PatrolStatus _currentPatrolStatus;
        /// <summary>
        /// Gets or sets the current patrol status.
        /// </summary>
        public PatrolStatus CurrentPatrolStatus
        {
            get => _currentPatrolStatus;
            set => SetProperty(ref _currentPatrolStatus, value, onChanged: UpdateCompletionPercentage);
        }

        private bool _isPatrolActive;
        /// <summary>
        /// Gets or sets a value indicating whether a patrol is currently active.
        /// </summary>
        public bool IsPatrolActive
        {
            get => _isPatrolActive;
            set => SetProperty(ref _isPatrolActive, value);
        }

        private bool _isLocationSelected;
        /// <summary>
        /// Gets or sets a value indicating whether a location has been selected.
        /// </summary>
        public bool IsLocationSelected
        {
            get => _isLocationSelected;
            set => SetProperty(ref _isLocationSelected, value);
        }

        private bool _canVerifyCheckpoint;
        /// <summary>
        /// Gets or sets a value indicating whether the user can verify a checkpoint.
        /// </summary>
        public bool CanVerifyCheckpoint
        {
            get => _canVerifyCheckpoint;
            set => SetProperty(ref _canVerifyCheckpoint, value);
        }

        private double _completionPercentage;
        /// <summary>
        /// Gets or sets the percentage of checkpoints that have been verified.
        /// </summary>
        public double CompletionPercentage
        {
            get => _completionPercentage;
            set => SetProperty(ref _completionPercentage, value);
        }

        private string _statusMessage;
        /// <summary>
        /// Gets or sets the status message to display to the user.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private object _mapView;
        /// <summary>
        /// Gets or sets the map view object for map initialization.
        /// </summary>
        public object MapView
        {
            get => _mapView;
            set => SetProperty(ref _mapView, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to select a patrol location.
        /// </summary>
        public ICommand SelectLocationCommand { get; }

        /// <summary>
        /// Command to start a patrol at the selected location.
        /// </summary>
        public ICommand StartPatrolCommand { get; }

        /// <summary>
        /// Command to end the current patrol.
        /// </summary>
        public ICommand EndPatrolCommand { get; }

        /// <summary>
        /// Command to verify a checkpoint.
        /// </summary>
        public ICommand VerifyCheckpointCommand { get; }

        /// <summary>
        /// Command to navigate to the checkpoint list view.
        /// </summary>
        public ICommand ViewCheckpointListCommand { get; }

        /// <summary>
        /// Command to refresh the patrol status and checkpoint data.
        /// </summary>
        public ICommand RefreshCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PatrolViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="authenticationStateProvider">The authentication state provider.</param>
        /// <param name="patrolService">The patrol service.</param>
        /// <param name="mapService">The map service.</param>
        /// <param name="locationService">The location service.</param>
        public PatrolViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IPatrolService patrolService,
            IMapService mapService,
            ILocationService locationService)
            : base(navigationService, authenticationStateProvider)
        {
            _patrolService = patrolService ?? throw new ArgumentNullException(nameof(patrolService));
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));

            // Initialize collections
            Locations = new ObservableCollection<LocationModel>();
            Checkpoints = new ObservableCollection<CheckpointModel>();
            CurrentPatrolStatus = new PatrolStatus();

            // Initialize properties
            Title = "Patrol";
            IsPatrolActive = _patrolService.IsPatrolActive;
            IsLocationSelected = false;
            CanVerifyCheckpoint = false;
            CompletionPercentage = 0;
            StatusMessage = "Select a location to begin patrol";

            // Initialize commands
            SelectLocationCommand = new AsyncRelayCommand<LocationModel>(SelectLocationAsync);
            StartPatrolCommand = new AsyncRelayCommand(StartPatrolAsync, () => IsLocationSelected && !IsPatrolActive);
            EndPatrolCommand = new AsyncRelayCommand(EndPatrolAsync, () => IsPatrolActive);
            VerifyCheckpointCommand = new AsyncRelayCommand<CheckpointModel>(VerifyCheckpointAsync, cp => CanVerifyCheckpoint && cp != null && !cp.IsVerified);
            ViewCheckpointListCommand = new AsyncRelayCommand(NavigateToCheckpointList, () => IsLocationSelected);
            RefreshCommand = new AsyncRelayCommand(async () => await ExecuteWithBusyIndicator(async () => 
            {
                if (IsPatrolActive && SelectedLocation != null)
                {
                    await UpdatePatrolStatusAsync();
                }
                else
                {
                    await LoadLocationsAsync();
                }
            }));

            // Subscribe to events
            _patrolService.CheckpointProximityChanged += HandleCheckpointProximityChanged;
            _locationService.LocationChanged += HandleLocationChanged;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the ViewModel by loading available patrol locations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await ExecuteWithBusyIndicator(async () =>
            {
                await LoadLocationsAsync();

                // Check if a patrol is already active
                if (_patrolService.IsPatrolActive && _patrolService.CurrentLocationId.HasValue)
                {
                    SelectedLocation = Locations.FirstOrDefault(l => l.Id == _patrolService.CurrentLocationId.Value);
                    
                    if (SelectedLocation != null)
                    {
                        IsLocationSelected = true;
                        IsPatrolActive = true;
                        
                        await LoadCheckpointsAsync(SelectedLocation.Id);
                        await UpdatePatrolStatusAsync();
                    }
                }
            });
        }

        /// <summary>
        /// Called when the view is navigated to.
        /// </summary>
        /// <param name="parameters">Navigation parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedTo(Dictionary<string, object> parameters)
        {
            await base.OnNavigatedTo(parameters);

            // Initialize map if provided
            if (parameters != null)
            {
                if (parameters.TryGetValue("MapView", out var mapView) && mapView != null)
                {
                    MapView = mapView;
                    await _mapService.InitializeMap(MapView);
                    _mapService.ShowUserLocation(true);
                }

                // Check if locationId parameter is provided
                if (parameters.TryGetValue(NavigationConstants.ParamLocationId, out var locationIdObj) && 
                    locationIdObj is int locationId)
                {
                    var location = Locations.FirstOrDefault(l => l.Id == locationId);
                    if (location != null)
                    {
                        await SelectLocationAsync(location);
                    }
                }
            }
        }

        /// <summary>
        /// Called when navigating away from the page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedFrom()
        {
            // Unsubscribe from events to prevent memory leaks
            _patrolService.CheckpointProximityChanged -= HandleCheckpointProximityChanged;
            _locationService.LocationChanged -= HandleLocationChanged;

            await base.OnNavigatedFrom();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads all available patrol locations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LoadLocationsAsync()
        {
            Locations.Clear();
            var locations = await _patrolService.GetLocations();
            foreach (var location in locations)
            {
                Locations.Add(location);
            }
        }

        /// <summary>
        /// Loads checkpoints for the specified location.
        /// </summary>
        /// <param name="locationId">The ID of the location to load checkpoints for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LoadCheckpointsAsync(int locationId)
        {
            Checkpoints.Clear();
            var checkpoints = await _patrolService.GetCheckpoints(locationId);
            foreach (var checkpoint in checkpoints)
            {
                Checkpoints.Add(checkpoint);
            }
        }

        /// <summary>
        /// Handles the selection of a patrol location.
        /// </summary>
        /// <param name="location">The selected location.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectLocationAsync(LocationModel location)
        {
            if (location == null) return;

            SelectedLocation = location;
            IsLocationSelected = true;

            // Load checkpoints for this location
            await LoadCheckpointsAsync(location.Id);

            // Update the map
            var currentLocation = await _locationService.GetCurrentLocation();
            if (currentLocation != null)
            {
                await _mapService.CenterMap(currentLocation.Latitude, currentLocation.Longitude, 200);
            }

            await _mapService.DisplayCheckpoints(Checkpoints);
            StatusMessage = "Start patrol to begin verification";
        }

        /// <summary>
        /// Starts a patrol at the selected location.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task StartPatrolAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                CurrentPatrolStatus = await _patrolService.StartPatrol(SelectedLocation.Id);
                IsPatrolActive = true;
                CompletionPercentage = CurrentPatrolStatus.CalculateCompletionPercentage();
                StatusMessage = "Move closer to a checkpoint to verify";
            }
            catch (Exception ex)
            {
                SetError($"Failed to start patrol: {ex.Message}");
            }
        }

        /// <summary>
        /// Ends the current patrol.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task EndPatrolAsync()
        {
            if (!IsPatrolActive) return;

            try
            {
                CurrentPatrolStatus = await _patrolService.EndPatrol();
                IsPatrolActive = false;
                CompletionPercentage = CurrentPatrolStatus.CalculateCompletionPercentage();
                StatusMessage = "Patrol ended";
            }
            catch (Exception ex)
            {
                SetError($"Failed to end patrol: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies a checkpoint when the user is within proximity.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to verify.</param>
        /// <returns>A task representing the asynchronous operation with a boolean indicating success.</returns>
        private async Task<bool> VerifyCheckpointAsync(CheckpointModel checkpoint)
        {
            if (checkpoint == null) return false;
            if (checkpoint.IsVerified) return true; // Already verified

            try
            {
                bool success = await _patrolService.VerifyCheckpoint(checkpoint.Id);
                
                if (success)
                {
                    // Update checkpoint status locally
                    checkpoint.MarkAsVerified();
                    
                    // Update UI
                    await _mapService.UpdateCheckpointStatus(checkpoint.Id, true);
                    await UpdatePatrolStatusAsync();
                    CanVerifyCheckpoint = false;
                }
                
                return success;
            }
            catch (Exception ex)
            {
                SetError($"Failed to verify checkpoint: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates the current patrol status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdatePatrolStatusAsync()
        {
            if (!IsPatrolActive || SelectedLocation == null) return;

            try
            {
                CurrentPatrolStatus = await _patrolService.GetPatrolStatus(SelectedLocation.Id);
                UpdateCompletionPercentage();
                
                if (CurrentPatrolStatus.IsComplete())
                {
                    StatusMessage = "Patrol complete!";
                }
                else
                {
                    UpdateStatusMessage();
                }
            }
            catch (Exception ex)
            {
                SetError($"Failed to update patrol status: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the checkpoint proximity changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void HandleCheckpointProximityChanged(object sender, CheckpointProximityEventArgs e)
        {
            // Find the checkpoint in our collection
            var checkpoint = Checkpoints.FirstOrDefault(c => c.Id == e.CheckpointId);
            if (checkpoint == null) return;

            // Only highlight and enable verification for unverified checkpoints
            if (!checkpoint.IsVerified)
            {
                _mapService.HighlightCheckpoint(checkpoint.Id, e.IsInRange);
                CanVerifyCheckpoint = e.IsInRange;
                SelectedCheckpoint = e.IsInRange ? checkpoint : null;
                
                if (e.IsInRange)
                {
                    StatusMessage = "Checkpoint in range, tap to verify";
                }
                else
                {
                    UpdateStatusMessage();
                }
            }
        }

        /// <summary>
        /// Handles the location changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (e.Location != null)
            {
                // Update user location on map
                _mapService.UpdateUserLocation(e.Location.Latitude, e.Location.Longitude);
            }
        }

        /// <summary>
        /// Navigates to the checkpoint list page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task NavigateToCheckpointList()
        {
            if (SelectedLocation == null) return;

            var parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamLocationId, SelectedLocation.Id }
            };

            await NavigationService.NavigateToAsync(NavigationConstants.CheckpointListPage, parameters);
        }

        /// <summary>
        /// Updates the completion percentage based on the current patrol status.
        /// </summary>
        private void UpdateCompletionPercentage()
        {
            if (CurrentPatrolStatus != null)
            {
                CompletionPercentage = CurrentPatrolStatus.CalculateCompletionPercentage();
            }
            else
            {
                CompletionPercentage = 0;
            }
        }

        /// <summary>
        /// Updates the status message based on the current state.
        /// </summary>
        private void UpdateStatusMessage()
        {
            if (!IsLocationSelected)
            {
                StatusMessage = "Select a location to begin patrol";
            }
            else if (!IsPatrolActive)
            {
                StatusMessage = "Start patrol to begin verification";
            }
            else if (CanVerifyCheckpoint && SelectedCheckpoint != null)
            {
                StatusMessage = "Checkpoint in range, tap to verify";
            }
            else if (CurrentPatrolStatus?.IsComplete() == true)
            {
                StatusMessage = "Patrol complete!";
            }
            else
            {
                StatusMessage = "Move closer to a checkpoint to verify";
            }
        }

        #endregion

        #region IDisposable

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

        #endregion
    }
}