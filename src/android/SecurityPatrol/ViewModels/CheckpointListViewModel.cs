using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Collections.ObjectModel; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Linq; // Version 8.0.0
using CommunityToolkit.Mvvm.ComponentModel; // Latest
using CommunityToolkit.Mvvm.Input; // Latest
using SecurityPatrol.ViewModels;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the checkpoint list screen that displays and manages a list of checkpoints for a selected patrol location.
    /// </summary>
    [ObservableObject]
    [QueryProperty(nameof(LocationId), nameof(LocationId))]
    public class CheckpointListViewModel : BaseViewModel
    {
        private readonly IPatrolService _patrolService;
        private readonly ILocationService _locationService;
        private int? _locationId;
        private ObservableCollection<CheckpointModel> _checkpoints;
        private ObservableCollection<CheckpointModel> _filteredCheckpoints;
        private CheckpointModel _selectedCheckpoint;
        private CheckpointModel _nearbyCheckpoint;
        private bool _isPatrolActive;
        private bool _isLoading;
        private bool _canVerifyCheckpoint;
        private string _filterText;
        private bool _showVerifiedCheckpoints;
        private bool _showUnverifiedCheckpoints;
        private int _totalCheckpoints;
        private int _verifiedCheckpoints;
        private double _completionPercentage;

        /// <summary>
        /// Gets the patrol service used for checkpoint management.
        /// </summary>
        public IPatrolService PatrolService => _patrolService;

        /// <summary>
        /// Gets the location service used for accessing location data.
        /// </summary>
        public ILocationService LocationService => _locationService;

        /// <summary>
        /// Gets or sets the ID of the selected patrol location.
        /// </summary>
        public int? LocationId
        {
            get => _locationId;
            set => SetProperty(ref _locationId, value, onChanged: async () => await LoadCheckpointsAsync());
        }

        /// <summary>
        /// Gets or sets the collection of all checkpoints for the selected location.
        /// </summary>
        public ObservableCollection<CheckpointModel> Checkpoints
        {
            get => _checkpoints;
            set => SetProperty(ref _checkpoints, value);
        }

        /// <summary>
        /// Gets or sets the filtered collection of checkpoints based on filter criteria.
        /// </summary>
        public ObservableCollection<CheckpointModel> FilteredCheckpoints
        {
            get => _filteredCheckpoints;
            set => SetProperty(ref _filteredCheckpoints, value);
        }

        /// <summary>
        /// Gets or sets the currently selected checkpoint.
        /// </summary>
        public CheckpointModel SelectedCheckpoint
        {
            get => _selectedCheckpoint;
            set => SetProperty(ref _selectedCheckpoint, value);
        }

        /// <summary>
        /// Gets or sets the checkpoint currently within proximity range.
        /// </summary>
        public CheckpointModel NearbyCheckpoint
        {
            get => _nearbyCheckpoint;
            set => SetProperty(ref _nearbyCheckpoint, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a patrol is currently active.
        /// </summary>
        public bool IsPatrolActive
        {
            get => _isPatrolActive;
            set => SetProperty(ref _isPatrolActive, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether data is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a checkpoint can be verified (when within proximity).
        /// </summary>
        public bool CanVerifyCheckpoint
        {
            get => _canVerifyCheckpoint;
            set => SetProperty(ref _canVerifyCheckpoint, value);
        }

        /// <summary>
        /// Gets or sets the text used to filter checkpoints by name.
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value, onChanged: () => ApplyFilter());
        }

        /// <summary>
        /// Gets or sets a value indicating whether verified checkpoints should be shown.
        /// </summary>
        public bool ShowVerifiedCheckpoints
        {
            get => _showVerifiedCheckpoints;
            set => SetProperty(ref _showVerifiedCheckpoints, value, onChanged: () => ApplyFilter());
        }

        /// <summary>
        /// Gets or sets a value indicating whether unverified checkpoints should be shown.
        /// </summary>
        public bool ShowUnverifiedCheckpoints
        {
            get => _showUnverifiedCheckpoints;
            set => SetProperty(ref _showUnverifiedCheckpoints, value, onChanged: () => ApplyFilter());
        }

        /// <summary>
        /// Gets or sets the total number of checkpoints for the selected location.
        /// </summary>
        public int TotalCheckpoints
        {
            get => _totalCheckpoints;
            set => SetProperty(ref _totalCheckpoints, value);
        }

        /// <summary>
        /// Gets or sets the number of verified checkpoints.
        /// </summary>
        public int VerifiedCheckpoints
        {
            get => _verifiedCheckpoints;
            set => SetProperty(ref _verifiedCheckpoints, value, onChanged: () => OnPropertyChanged(nameof(CompletionPercentage)));
        }

        /// <summary>
        /// Gets the percentage of checkpoint completion (0-100).
        /// </summary>
        public double CompletionPercentage
        {
            get => _completionPercentage;
            private set => SetProperty(ref _completionPercentage, value);
        }

        /// <summary>
        /// Initializes a new instance of the CheckpointListViewModel class with required services.
        /// </summary>
        /// <param name="navigationService">The navigation service for page navigation.</param>
        /// <param name="authenticationStateProvider">The authentication state provider for user authentication status.</param>
        /// <param name="patrolService">The patrol service for checkpoint management.</param>
        /// <param name="locationService">The location service for accessing user location.</param>
        /// <exception cref="ArgumentNullException">Thrown if required services are null.</exception>
        public CheckpointListViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IPatrolService patrolService,
            ILocationService locationService)
            : base(navigationService, authenticationStateProvider)
        {
            _patrolService = patrolService ?? throw new ArgumentNullException(nameof(patrolService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            
            Checkpoints = new ObservableCollection<CheckpointModel>();
            FilteredCheckpoints = new ObservableCollection<CheckpointModel>();
            
            Title = "Checkpoints";
            ShowVerifiedCheckpoints = true;
            ShowUnverifiedCheckpoints = true;
            
            // Subscribe to checkpoint proximity events
            _patrolService.CheckpointProximityChanged += OnCheckpointProximityChanged;
        }

        /// <summary>
        /// Initializes the ViewModel when navigated to.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            IsPatrolActive = PatrolService.IsPatrolActive;
            
            if (LocationId.HasValue)
            {
                await LoadCheckpointsAsync();
            }
        }

        /// <summary>
        /// Called when the page using this ViewModel appears.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnAppearing()
        {
            await base.OnAppearing();
            
            // Reload checkpoints when page appears, in case changes were made elsewhere
            if (LocationId.HasValue)
            {
                await LoadCheckpointsAsync();
            }
        }

        /// <summary>
        /// Called when the page using this ViewModel disappears.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisappearing()
        {
            await base.OnDisappearing();
        }

        /// <summary>
        /// Loads checkpoints for the selected location.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadCheckpointsAsync()
        {
            if (!LocationId.HasValue)
            {
                return;
            }

            IsLoading = true;
            ClearError();
            
            try
            {
                // Clear existing collections
                Checkpoints.Clear();
                FilteredCheckpoints.Clear();
                
                // Fetch checkpoints from the service
                var checkpoints = await PatrolService.GetCheckpoints(LocationId.Value);
                
                // Add to collection
                foreach (var checkpoint in checkpoints)
                {
                    Checkpoints.Add(checkpoint);
                }
                
                // Update stats
                TotalCheckpoints = Checkpoints.Count;
                VerifiedCheckpoints = Checkpoints.Count(c => c.IsVerified);
                
                // Calculate completion percentage
                if (TotalCheckpoints > 0)
                {
                    CompletionPercentage = ((double)VerifiedCheckpoints / TotalCheckpoints) * 100;
                }
                else
                {
                    CompletionPercentage = 0;
                }
                
                // Apply filter to update filtered checkpoints
                ApplyFilter();
                
                // Update distances (if possible)
                await UpdateCheckpointDistances();
            }
            catch (Exception ex)
            {
                SetError($"Error loading checkpoints: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applies the current filter settings to the checkpoints collection.
        /// </summary>
        private void ApplyFilter()
        {
            FilteredCheckpoints.Clear();
            
            var filtered = Checkpoints.AsEnumerable();
            
            // Apply text filter if provided
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                filtered = filtered.Where(c => c.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
            }
            
            // Apply verified/unverified filters
            if (ShowVerifiedCheckpoints && !ShowUnverifiedCheckpoints)
            {
                filtered = filtered.Where(c => c.IsVerified);
            }
            else if (!ShowVerifiedCheckpoints && ShowUnverifiedCheckpoints)
            {
                filtered = filtered.Where(c => !c.IsVerified);
            }
            else if (!ShowVerifiedCheckpoints && !ShowUnverifiedCheckpoints)
            {
                // If both are unchecked, show nothing
                filtered = Enumerable.Empty<CheckpointModel>();
            }
            
            // Add filtered checkpoints to collection
            foreach (var checkpoint in filtered)
            {
                FilteredCheckpoints.Add(checkpoint);
            }
        }

        /// <summary>
        /// Called when the filter text changes.
        /// </summary>
        [RelayCommand]
        private void OnFilterTextChanged()
        {
            ApplyFilter();
        }

        /// <summary>
        /// Toggles the filter for verified checkpoints.
        /// </summary>
        [RelayCommand]
        private void ToggleVerifiedFilter()
        {
            ShowVerifiedCheckpoints = !ShowVerifiedCheckpoints;
        }

        /// <summary>
        /// Toggles the filter for unverified checkpoints.
        /// </summary>
        [RelayCommand]
        private void ToggleUnverifiedFilter()
        {
            ShowUnverifiedCheckpoints = !ShowUnverifiedCheckpoints;
        }

        /// <summary>
        /// Handles selection of a checkpoint.
        /// </summary>
        /// <param name="checkpoint">The selected checkpoint.</param>
        [RelayCommand]
        private void OnCheckpointSelected(CheckpointModel checkpoint)
        {
            SelectedCheckpoint = checkpoint;
        }

        /// <summary>
        /// Verifies the selected checkpoint as completed.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to verify.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task VerifyCheckpointAsync(CheckpointModel checkpoint)
        {
            if (checkpoint == null || checkpoint.IsVerified)
            {
                return;
            }
            
            if (!IsPatrolActive)
            {
                SetError("Cannot verify checkpoint: No active patrol.");
                return;
            }
            
            await ExecuteWithBusyIndicator(async () =>
            {
                bool success = await PatrolService.VerifyCheckpoint(checkpoint.Id);
                
                if (success)
                {
                    // Update checkpoint as verified
                    checkpoint.IsVerified = true;
                    checkpoint.VerificationTime = DateTime.UtcNow;
                    
                    // Update stats
                    VerifiedCheckpoints++;
                    CompletionPercentage = ((double)VerifiedCheckpoints / TotalCheckpoints) * 100;
                    
                    // Update filtered checkpoints to reflect changes
                    ApplyFilter();
                    
                    // Clear nearby checkpoint if it's the one we just verified
                    if (NearbyCheckpoint?.Id == checkpoint.Id)
                    {
                        NearbyCheckpoint = null;
                        CanVerifyCheckpoint = false;
                    }
                }
            });
        }

        /// <summary>
        /// Navigates to the checkpoint verification page.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to verify.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task NavigateToVerificationAsync(CheckpointModel checkpoint)
        {
            if (checkpoint == null)
            {
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamCheckpointId, checkpoint.Id }
            };
            
            await NavigationService.NavigateToAsync(NavigationConstants.VerificationPage, parameters);
        }

        /// <summary>
        /// Refreshes the checkpoint list.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task RefreshCheckpointsAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                await LoadCheckpointsAsync();
            });
        }

        /// <summary>
        /// Handles checkpoint proximity change events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCheckpointProximityChanged(object sender, CheckpointProximityEventArgs e)
        {
            if (e.IsInRange)
            {
                // Find the checkpoint in our collection
                var checkpoint = Checkpoints.FirstOrDefault(c => c.Id == e.CheckpointId);
                
                if (checkpoint != null)
                {
                    NearbyCheckpoint = checkpoint;
                    CanVerifyCheckpoint = !checkpoint.IsVerified;
                }
            }
            else if (NearbyCheckpoint != null && NearbyCheckpoint.Id == e.CheckpointId)
            {
                // Clear nearby checkpoint if we're moving away from it
                NearbyCheckpoint = null;
                CanVerifyCheckpoint = false;
            }
        }

        /// <summary>
        /// Updates the distance to each checkpoint based on current location.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateCheckpointDistances()
        {
            try
            {
                // Get current location
                var location = await LocationService.GetCurrentLocation();
                
                if (location == null)
                {
                    return;
                }
                
                // Calculate distances for all checkpoints
                // Note: The distance is calculated but not stored as a property since
                // CheckpointModel doesn't have a Distance property. The calculation
                // is available through the CalculateDistance method when needed.
                foreach (var checkpoint in Checkpoints)
                {
                    checkpoint.CalculateDistance(location.Latitude, location.Longitude);
                }
            }
            catch (Exception)
            {
                // Silent fail - updating distances is a nice-to-have feature
            }
        }

        /// <summary>
        /// Disposes of resources used by the ViewModel.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from events
            if (_patrolService != null)
            {
                _patrolService.CheckpointProximityChanged -= OnCheckpointProximityChanged;
            }
            
            base.Dispose();
        }
    }
}