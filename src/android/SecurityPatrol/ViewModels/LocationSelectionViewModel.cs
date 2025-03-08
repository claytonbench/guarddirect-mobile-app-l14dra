using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the location selection screen, handling loading and displaying available patrol locations
    /// and navigating to the patrol screen with the selected location.
    /// </summary>
    public class LocationSelectionViewModel : BaseViewModel
    {
        private ObservableCollection<LocationModel> _locations;
        private LocationModel _selectedLocation;
        private bool _hasLocations;

        /// <summary>
        /// Gets the patrol service used to retrieve location data.
        /// </summary>
        protected IPatrolService PatrolService { get; }

        /// <summary>
        /// Gets the collection of available patrol locations.
        /// </summary>
        public ObservableCollection<LocationModel> Locations
        {
            get => _locations;
            private set => SetProperty(ref _locations, value);
        }

        /// <summary>
        /// Gets or sets the currently selected location.
        /// </summary>
        public LocationModel SelectedLocation
        {
            get => _selectedLocation;
            set => SetProperty(ref _selectedLocation, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether any locations are available.
        /// </summary>
        public bool HasLocations
        {
            get => _hasLocations;
            private set => SetProperty(ref _hasLocations, value);
        }

        /// <summary>
        /// Gets the command to select a location and navigate to patrol page.
        /// </summary>
        public ICommand SelectLocationCommand { get; }

        /// <summary>
        /// Gets the command to refresh the list of locations.
        /// </summary>
        public ICommand RefreshLocationsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationSelectionViewModel"/> class with required services.
        /// </summary>
        /// <param name="navigationService">Service for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service for accessing authentication state.</param>
        /// <param name="patrolService">Service for retrieving patrol locations.</param>
        /// <exception cref="ArgumentNullException">Thrown if patrolService is null.</exception>
        public LocationSelectionViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IPatrolService patrolService)
            : base(navigationService, authenticationStateProvider)
        {
            PatrolService = patrolService ?? throw new ArgumentNullException(nameof(patrolService));
            
            Locations = new ObservableCollection<LocationModel>();
            SelectLocationCommand = new RelayCommand<LocationModel>(SelectLocation, CanSelectLocation);
            RefreshLocationsCommand = new AsyncRelayCommand(LoadLocationsAsync);
            
            Title = "Select Location";
        }

        /// <summary>
        /// Initializes the ViewModel when navigated to.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadLocationsAsync();
        }

        /// <summary>
        /// Loads available patrol locations from the service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadLocationsAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                Locations.Clear();
                
                var locations = await PatrolService.GetLocations();
                
                if (locations != null)
                {
                    foreach (var location in locations)
                    {
                        Locations.Add(location);
                    }
                }
                
                UpdateHasLocations();
            });
        }

        /// <summary>
        /// Handles selection of a patrol location and navigates to the patrol page.
        /// </summary>
        /// <param name="location">The selected location.</param>
        private void SelectLocation(LocationModel location)
        {
            if (location == null)
                return;

            SelectedLocation = location;
            
            var parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamLocationId, location.Id }
            };
            
            NavigationService.NavigateToAsync(NavigationConstants.PatrolPage, parameters);
        }

        /// <summary>
        /// Determines whether a location can be selected.
        /// </summary>
        /// <param name="location">The location to evaluate.</param>
        /// <returns>True if the location can be selected, otherwise false.</returns>
        private bool CanSelectLocation(LocationModel location)
        {
            return location != null;
        }

        /// <summary>
        /// Handles property change notifications.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            
            if (propertyName == nameof(Locations))
            {
                UpdateHasLocations();
            }
        }

        /// <summary>
        /// Updates the HasLocations property based on the Locations collection.
        /// </summary>
        private void UpdateHasLocations()
        {
            HasLocations = Locations?.Count > 0;
        }
    }
}