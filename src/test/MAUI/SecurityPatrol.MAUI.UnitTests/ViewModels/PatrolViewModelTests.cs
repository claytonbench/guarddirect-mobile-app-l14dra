using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using System.Linq; // System.Linq 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using System.Windows.Input; // System.Windows.Input 8.0+
using CommunityToolkit.Mvvm.ComponentModel; // CommunityToolkit.Mvvm 8.0+
using CommunityToolkit.Mvvm.Input; // CommunityToolkit.Mvvm 8.0+
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;
using Xunit; // Xunit 2.4.2
using Moq; // Moq 4.18.4
using FluentAssertions; // FluentAssertions 6.11.0
using SecurityPatrol.MAUI.UnitTests.Setup;

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    /// <summary>
    /// Test class for PatrolViewModel, containing unit tests for patrol operations, checkpoint management, and verification functionality.
    /// </summary>
    public class PatrolViewModelTests : TestBase
    {
        /// <summary>
        /// Initializes a new instance of the PatrolViewModelTests class
        /// </summary>
        public PatrolViewModelTests()
        {
            // Initialize MockAuthStateProvider with a new instance of Mock<IAuthenticationStateProvider>
            MockAuthStateProvider = new Mock<IAuthenticationStateProvider>();

            // Setup MockAuthStateProvider to return an authenticated state
            MockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(CreateAuthenticatedState());
        }

        /// <summary>
        /// Gets or sets the mock authentication state provider.
        /// </summary>
        public Mock<IAuthenticationStateProvider> MockAuthStateProvider { get; set; }

        /// <summary>
        /// Creates a new instance of PatrolViewModel with mocked dependencies for testing
        /// </summary>
        /// <returns>A new PatrolViewModel instance with mocked dependencies</returns>
        private PatrolViewModel CreateViewModel()
        {
            // Create a new PatrolViewModel with MockNavigationService.Object, MockAuthStateProvider.Object, MockPatrolService.Object, MockMapService.Object, and MockLocationService.Object
            var viewModel = new PatrolViewModel(MockNavigationService.Object, MockAuthStateProvider.Object, MockPatrolService.Object, MockMapService.Object, MockLocationService.Object);

            // Return the created ViewModel
            return viewModel;
        }

        /// <summary>
        /// Configures the mock patrol service with default behaviors for testing
        /// </summary>
        private void SetupMockPatrolService()
        {
            // Setup MockPatrolService.Setup(x => x.GetLocations()).ReturnsAsync(new List<LocationModel>())
            MockPatrolService.Setup(x => x.GetLocations()).ReturnsAsync(new List<LocationModel>());

            // Setup MockPatrolService.Setup(x => x.GetCheckpoints(It.IsAny<int>())).ReturnsAsync(new List<CheckpointModel>())
            MockPatrolService.Setup(x => x.GetCheckpoints(It.IsAny<int>())).ReturnsAsync(new List<CheckpointModel>());

            // Setup MockPatrolService.Setup(x => x.VerifyCheckpoint(It.IsAny<int>())).ReturnsAsync(true)
            MockPatrolService.Setup(x => x.VerifyCheckpoint(It.IsAny<int>())).ReturnsAsync(true);

            // Setup MockPatrolService.Setup(x => x.GetPatrolStatus(It.IsAny<int>())).ReturnsAsync(new PatrolStatus())
            MockPatrolService.Setup(x => x.GetPatrolStatus(It.IsAny<int>())).ReturnsAsync(new PatrolStatus());

            // Setup MockPatrolService.Setup(x => x.StartPatrol(It.IsAny<int>())).ReturnsAsync(new PatrolStatus())
            MockPatrolService.Setup(x => x.StartPatrol(It.IsAny<int>())).ReturnsAsync(new PatrolStatus());

            // Setup MockPatrolService.Setup(x => x.EndPatrol()).ReturnsAsync(new PatrolStatus())
            MockPatrolService.Setup(x => x.EndPatrol()).ReturnsAsync(new PatrolStatus());

            // Setup MockPatrolService.SetupGet(x => x.IsPatrolActive).Returns(false)
            MockPatrolService.SetupGet(x => x.IsPatrolActive).Returns(false);
        }

        /// <summary>
        /// Configures the mock map service with default behaviors for testing
        /// </summary>
        private void SetupMockMapService()
        {
            // Setup MockMapService.Setup(x => x.InitializeMap(It.IsAny<object>())).Returns(Task.CompletedTask)
            MockMapService.Setup(x => x.InitializeMap(It.IsAny<object>())).Returns(Task.CompletedTask);

            // Setup MockMapService.Setup(x => x.ShowUserLocation(It.IsAny<bool>())).Verifiable()
            MockMapService.Setup(x => x.ShowUserLocation(It.IsAny<bool>())).Verifiable();

            // Setup MockMapService.Setup(x => x.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>())).Returns(Task.CompletedTask)
            MockMapService.Setup(x => x.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>())).Returns(Task.CompletedTask);

            // Setup MockMapService.Setup(x => x.CenterMap(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(Task.CompletedTask)
            MockMapService.Setup(x => x.CenterMap(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(Task.CompletedTask);

            // Setup MockMapService.Setup(x => x.HighlightCheckpoint(It.IsAny<int>(), It.IsAny<bool>())).Verifiable()
            MockMapService.Setup(x => x.HighlightCheckpoint(It.IsAny<int>(), It.IsAny<bool>())).Verifiable();

            // Setup MockMapService.Setup(x => x.UpdateCheckpointStatus(It.IsAny<int>(), It.IsAny<bool>())).Returns(Task.CompletedTask)
            MockMapService.Setup(x => x.UpdateCheckpointStatus(It.IsAny<int>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Setup MockMapService.Setup(x => x.UpdateUserLocation(It.IsAny<double>(), It.IsAny<double>())).Returns(Task.CompletedTask)
            MockMapService.Setup(x => x.UpdateUserLocation(It.IsAny<double>(), It.IsAny<double>())).Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Configures the mock location service with default behaviors for testing
        /// </summary>
        private void SetupMockLocationService()
        {
            // Setup MockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 })
            MockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });
        }

        /// <summary>
        /// Creates a list of test location models for testing
        /// </summary>
        /// <param name="count">The number of locations to create</param>
        /// <returns>A list of test location models</returns>
        private List<LocationModel> CreateTestLocations(int count)
        {
            // Create a new List<LocationModel>
            var locations = new List<LocationModel>();

            // Add 'count' number of LocationModel instances with test data
            for (int i = 1; i <= count; i++)
            {
                locations.Add(new LocationModel { Id = i, Name = $"Location {i}", Latitude = 37.7749 + i, Longitude = -122.4194 + i });
            }

            // Return the list of locations
            return locations;
        }

        /// <summary>
        /// Creates a list of test checkpoint models for testing
        /// </summary>
        /// <param name="locationId">The location ID for the checkpoints</param>
        /// <param name="count">The number of checkpoints to create</param>
        /// <returns>A list of test checkpoint models</returns>
        private List<CheckpointModel> CreateTestCheckpoints(int locationId, int count)
        {
            // Create a new List<CheckpointModel>
            var checkpoints = new List<CheckpointModel>();

            // Add 'count' number of CheckpointModel instances with test data and the specified locationId
            for (int i = 1; i <= count; i++)
            {
                checkpoints.Add(new CheckpointModel { Id = i, LocationId = locationId, Name = $"Checkpoint {i}", Latitude = 37.7749 + i, Longitude = -122.4194 + i });
            }

            // Return the list of checkpoints
            return checkpoints;
        }

        /// <summary>
        /// Creates a test patrol status with specified parameters
        /// </summary>
        /// <param name="locationId">The location ID for the patrol status</param>
        /// <param name="totalCheckpoints">The total number of checkpoints</param>
        /// <param name="verifiedCheckpoints">The number of verified checkpoints</param>
        /// <returns>A test patrol status instance</returns>
        private PatrolStatus CreateTestPatrolStatus(int locationId, int totalCheckpoints, int verifiedCheckpoints)
        {
            // Create a new PatrolStatus instance
            var patrolStatus = new PatrolStatus();

            // Set LocationId to the specified locationId
            patrolStatus.LocationId = locationId;

            // Set TotalCheckpoints to the specified totalCheckpoints
            patrolStatus.TotalCheckpoints = totalCheckpoints;

            // Set VerifiedCheckpoints to the specified verifiedCheckpoints
            patrolStatus.VerifiedCheckpoints = verifiedCheckpoints;

            // Return the patrol status
            return patrolStatus;
        }

        /// <summary>
        /// Tests that InitializeAsync loads locations from the patrol service
        /// </summary>
        [Fact]
        public async Task Test_Initialize_LoadsLocations()
        {
            // Setup test locations
            var testLocations = CreateTestLocations(3);

            // Configure MockPatrolService to return test locations
            MockPatrolService.Setup(x => x.GetLocations()).ReturnsAsync(testLocations);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call InitializeAsync on the ViewModel
            await viewModel.InitializeAsync();

            // Verify that MockPatrolService.GetLocations was called
            MockPatrolService.Verify(x => x.GetLocations(), Times.Once);

            // Assert that ViewModel.Locations contains the test locations
            viewModel.Locations.Should().BeEquivalentTo(testLocations);
        }

        /// <summary>
        /// Tests that InitializeAsync loads checkpoints and status when a patrol is active
        /// </summary>
        [Fact]
        public async Task Test_Initialize_WhenPatrolActive_LoadsCheckpointsAndStatus()
        {
            // Setup test locations, checkpoints, and patrol status
            var testLocations = CreateTestLocations(1);
            var testCheckpoints = CreateTestCheckpoints(testLocations[0].Id, 5);
            var testPatrolStatus = CreateTestPatrolStatus(testLocations[0].Id, 5, 2);

            // Configure MockPatrolService to indicate an active patrol
            MockPatrolService.SetupGet(x => x.IsPatrolActive).Returns(true);

            // Configure MockPatrolService to return test data
            MockPatrolService.Setup(x => x.GetLocations()).ReturnsAsync(testLocations);
            MockPatrolService.Setup(x => x.GetCheckpoints(testLocations[0].Id)).ReturnsAsync(testCheckpoints);
            MockPatrolService.Setup(x => x.GetPatrolStatus(testLocations[0].Id)).ReturnsAsync(testPatrolStatus);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call InitializeAsync on the ViewModel
            await viewModel.InitializeAsync();

            // Verify that MockPatrolService.GetCheckpoints was called
            MockPatrolService.Verify(x => x.GetCheckpoints(testLocations[0].Id), Times.Once);

            // Verify that MockPatrolService.GetPatrolStatus was called
            MockPatrolService.Verify(x => x.GetPatrolStatus(testLocations[0].Id), Times.Once);

            // Assert that ViewModel.Checkpoints contains the test checkpoints
            viewModel.Checkpoints.Should().BeEquivalentTo(testCheckpoints);

            // Assert that ViewModel.CurrentPatrolStatus matches the test patrol status
            viewModel.CurrentPatrolStatus.Should().BeEquivalentTo(testPatrolStatus);

            // Assert that ViewModel.IsPatrolActive is true
            viewModel.IsPatrolActive.Should().BeTrue();
        }

        /// <summary>
        /// Tests that SelectLocationCommand loads checkpoints for the selected location
        /// </summary>
        [Fact]
        public async Task Test_SelectLocationCommand_LoadsCheckpoints()
        {
            // Setup test locations and checkpoints
            var testLocations = CreateTestLocations(1);
            var testCheckpoints = CreateTestCheckpoints(testLocations[0].Id, 5);

            // Configure MockPatrolService to return test data
            MockPatrolService.Setup(x => x.GetCheckpoints(testLocations[0].Id)).ReturnsAsync(testCheckpoints);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call InitializeAsync on the ViewModel
            await viewModel.InitializeAsync();

            // Execute SelectLocationCommand with a test location
            await viewModel.SelectLocationCommand.ExecuteAsync(testLocations[0]);

            // Verify that MockPatrolService.GetCheckpoints was called with the correct locationId
            MockPatrolService.Verify(x => x.GetCheckpoints(testLocations[0].Id), Times.Once);

            // Assert that ViewModel.Checkpoints contains the test checkpoints
            viewModel.Checkpoints.Should().BeEquivalentTo(testCheckpoints);

            // Assert that ViewModel.SelectedLocation is set to the test location
            viewModel.SelectedLocation.Should().BeEquivalentTo(testLocations[0]);

            // Assert that ViewModel.IsLocationSelected is true
            viewModel.IsLocationSelected.Should().BeTrue();
        }

        /// <summary>
        /// Tests that StartPatrolCommand starts a patrol for the selected location
        /// </summary>
        [Fact]
        public async Task Test_StartPatrolCommand_StartsPatrol()
        {
            // Setup test location and patrol status
            var testLocation = new LocationModel { Id = 1, Name = "Test Location" };
            var testPatrolStatus = CreateTestPatrolStatus(testLocation.Id, 5, 0);

            // Configure MockPatrolService to return test patrol status
            MockPatrolService.Setup(x => x.StartPatrol(testLocation.Id)).ReturnsAsync(testPatrolStatus);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Set ViewModel.SelectedLocation to the test location
            viewModel.SelectedLocation = testLocation;

            // Execute StartPatrolCommand
            await viewModel.StartPatrolCommand.ExecuteAsync(null);

            // Verify that MockPatrolService.StartPatrol was called with the correct locationId
            MockPatrolService.Verify(x => x.StartPatrol(testLocation.Id), Times.Once);

            // Assert that ViewModel.CurrentPatrolStatus matches the test patrol status
            viewModel.CurrentPatrolStatus.Should().BeEquivalentTo(testPatrolStatus);

            // Assert that ViewModel.IsPatrolActive is true
            viewModel.IsPatrolActive.Should().BeTrue();
        }

        /// <summary>
        /// Tests that EndPatrolCommand ends the current patrol
        /// </summary>
        [Fact]
        public async Task Test_EndPatrolCommand_EndsPatrol()
        {
            // Setup test patrol status
            var testPatrolStatus = CreateTestPatrolStatus(1, 5, 5);

            // Configure MockPatrolService to indicate an active patrol
            MockPatrolService.SetupGet(x => x.IsPatrolActive).Returns(true);

            // Configure MockPatrolService to return test patrol status
            MockPatrolService.Setup(x => x.EndPatrol()).ReturnsAsync(testPatrolStatus);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Set ViewModel.IsPatrolActive to true
            viewModel.IsPatrolActive = true;

            // Execute EndPatrolCommand
            await viewModel.EndPatrolCommand.ExecuteAsync(null);

            // Verify that MockPatrolService.EndPatrol was called
            MockPatrolService.Verify(x => x.EndPatrol(), Times.Once);

            // Assert that ViewModel.IsPatrolActive is false
            viewModel.IsPatrolActive.Should().BeFalse();
        }

        /// <summary>
        /// Tests that VerifyCheckpointCommand verifies the selected checkpoint
        /// </summary>
        [Fact]
        public async Task Test_VerifyCheckpointCommand_VerifiesCheckpoint()
        {
            // Setup test checkpoint and patrol status
            var testCheckpoint = new CheckpointModel { Id = 1, LocationId = 1, Name = "Test Checkpoint" };
            var testPatrolStatus = CreateTestPatrolStatus(1, 5, 1);

            // Configure MockPatrolService to return successful verification
            MockPatrolService.Setup(x => x.VerifyCheckpoint(testCheckpoint.Id)).ReturnsAsync(true);

            // Configure MockPatrolService to return updated patrol status
            MockPatrolService.Setup(x => x.GetPatrolStatus(1)).ReturnsAsync(testPatrolStatus);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Set ViewModel.SelectedCheckpoint to the test checkpoint
            viewModel.SelectedCheckpoint = testCheckpoint;

            // Set ViewModel.CanVerifyCheckpoint to true
            viewModel.CanVerifyCheckpoint = true;

            // Execute VerifyCheckpointCommand
            await viewModel.VerifyCheckpointCommand.ExecuteAsync(testCheckpoint);

            // Verify that MockPatrolService.VerifyCheckpoint was called with the correct checkpointId
            MockPatrolService.Verify(x => x.VerifyCheckpoint(testCheckpoint.Id), Times.Once);

            // Verify that MockMapService.UpdateCheckpointStatus was called
            MockMapService.Verify(x => x.UpdateCheckpointStatus(testCheckpoint.Id, true), Times.Once);

            // Assert that the checkpoint IsVerified property is true
            testCheckpoint.IsVerified.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ViewCheckpointListCommand navigates to the checkpoint list page
        /// </summary>
        [Fact]
        public async Task Test_ViewCheckpointListCommand_NavigatesToCheckpointList()
        {
            // Setup test location
            var testLocation = new LocationModel { Id = 1, Name = "Test Location" };

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Set ViewModel.SelectedLocation to the test location
            viewModel.SelectedLocation = testLocation;

            // Execute ViewCheckpointListCommand
            await viewModel.ViewCheckpointListCommand.ExecuteAsync(null);

            // Verify that MockNavigationService.NavigateToAsync was called with the correct route and parameters
            MockNavigationService.Verify(x => x.NavigateToAsync(
                NavigationConstants.CheckpointListPage,
                It.Is<Dictionary<string, object>>(p => p.ContainsKey(NavigationConstants.ParamLocationId) && (int)p[NavigationConstants.ParamLocationId] == testLocation.Id)
            ), Times.Once);
        }

        /// <summary>
        /// Tests that OnNavigatedTo initializes the map when a map view is provided
        /// </summary>
        [Fact]
        public async Task Test_OnNavigatedTo_WithMapView_InitializesMap()
        {
            // Create a test map view object
            var mapView = new object();

            // Create navigation parameters with the map view
            var parameters = new Dictionary<string, object>
            {
                { "MapView", mapView }
            };

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call OnNavigatedTo with the parameters
            await viewModel.OnNavigatedTo(parameters);

            // Verify that MockMapService.InitializeMap was called with the map view
            MockMapService.Verify(x => x.InitializeMap(mapView), Times.Once);
        }

        /// <summary>
        /// Tests that OnNavigatedTo selects a location when a location ID is provided
        /// </summary>
        [Fact]
        public async Task Test_OnNavigatedTo_WithLocationId_SelectsLocation()
        {
            // Setup test locations and checkpoints
            var testLocations = CreateTestLocations(1);
            var testCheckpoints = CreateTestCheckpoints(testLocations[0].Id, 5);

            // Configure MockPatrolService to return test data
            MockPatrolService.Setup(x => x.GetLocations()).ReturnsAsync(testLocations);
            MockPatrolService.Setup(x => x.GetCheckpoints(testLocations[0].Id)).ReturnsAsync(testCheckpoints);

            // Create navigation parameters with a location ID
            var parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamLocationId, testLocations[0].Id }
            };

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call InitializeAsync on the ViewModel
            await viewModel.InitializeAsync();

            // Call OnNavigatedTo with the parameters
            await viewModel.OnNavigatedTo(parameters);

            // Verify that MockPatrolService.GetCheckpoints was called with the correct locationId
            MockPatrolService.Verify(x => x.GetCheckpoints(testLocations[0].Id), Times.Once);

            // Assert that ViewModel.SelectedLocation is set to the correct location
            viewModel.SelectedLocation.Should().BeEquivalentTo(testLocations[0]);

            // Assert that ViewModel.IsLocationSelected is true
            viewModel.IsLocationSelected.Should().BeTrue();
        }

        /// <summary>
        /// Tests that the checkpoint proximity changed event handler updates the CanVerifyCheckpoint property
        /// </summary>
        [Fact]
        public async Task Test_HandleCheckpointProximityChanged_UpdatesCanVerifyCheckpoint()
        {
            // Setup test checkpoints
            var testCheckpoints = CreateTestCheckpoints(1, 1);

            // Configure MockPatrolService to return test checkpoints
            MockPatrolService.Setup(x => x.GetCheckpoints(1)).ReturnsAsync(testCheckpoints);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call InitializeAsync on the ViewModel
            await viewModel.InitializeAsync();

            // Create a CheckpointProximityEventArgs with IsInRange=true
            var proximityEventArgs = new CheckpointProximityEventArgs(testCheckpoints[0].Id, 10, true);

            // Raise the CheckpointProximityChanged event on MockPatrolService
            _patrolService.CheckpointProximityChanged += viewModel.HandleCheckpointProximityChanged;
            _patrolService.CheckpointProximityChanged?.Invoke(this, proximityEventArgs);

            // Assert that ViewModel.CanVerifyCheckpoint is true
            viewModel.CanVerifyCheckpoint.Should().BeTrue();

            // Assert that ViewModel.SelectedCheckpoint is set to the correct checkpoint
            viewModel.SelectedCheckpoint.Should().BeEquivalentTo(testCheckpoints[0]);
        }

        /// <summary>
        /// Tests that the location changed event handler updates the user location on the map
        /// </summary>
        [Fact]
        public async Task Test_HandleLocationChanged_UpdatesUserLocation()
        {
            // Create a test location
            var testLocation = new LocationModel { Latitude = 37.7749, Longitude = -122.4194 };

            // Create a LocationChangedEventArgs with the test location
            var locationChangedEventArgs = new LocationChangedEventArgs(testLocation);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Raise the LocationChanged event on MockLocationService
            _locationService.LocationChanged += viewModel.HandleLocationChanged;
            _locationService.LocationChanged?.Invoke(this, locationChangedEventArgs);

            // Verify that MockMapService.UpdateUserLocation was called with the correct coordinates
            MockMapService.Verify(x => x.UpdateUserLocation(testLocation.Latitude, testLocation.Longitude), Times.Once);
        }

        /// <summary>
        /// Tests that RefreshCommand reloads locations and checkpoints
        /// </summary>
        [Fact]
        public async Task Test_RefreshCommand_ReloadsData()
        {
            // Setup test locations and checkpoints
            var testLocations = CreateTestLocations(3);
            var testCheckpoints = CreateTestCheckpoints(testLocations[0].Id, 5);

            // Configure MockPatrolService to return test data
            MockPatrolService.Setup(x => x.GetLocations()).ReturnsAsync(testLocations);
            MockPatrolService.Setup(x => x.GetCheckpoints(testLocations[0].Id)).ReturnsAsync(testCheckpoints);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Set ViewModel.SelectedLocation to a test location
            viewModel.SelectedLocation = testLocations[0];

            // Execute RefreshCommand
            await viewModel.RefreshCommand.ExecuteAsync(null);

            // Verify that MockPatrolService.GetLocations was called
            MockPatrolService.Verify(x => x.GetLocations(), Times.Once);

            // Verify that MockPatrolService.GetCheckpoints was called with the correct locationId
            MockPatrolService.Verify(x => x.GetCheckpoints(testLocations[0].Id), Times.Once);

            // Assert that ViewModel.Locations contains the test locations
            viewModel.Locations.Should().BeEquivalentTo(testLocations);

            // Assert that ViewModel.Checkpoints contains the test checkpoints
            viewModel.Checkpoints.Should().BeEquivalentTo(testCheckpoints);
        }

        /// <summary>
        /// Tests that the CompletionPercentage property calculates correctly based on patrol status
        /// </summary>
        [Fact]
        public void Test_CompletionPercentage_CalculatesCorrectly()
        {
            // Setup test patrol status with 10 total checkpoints and 5 verified
            var testPatrolStatus = CreateTestPatrolStatus(1, 10, 5);

            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Set ViewModel.CurrentPatrolStatus to the test patrol status
            viewModel.CurrentPatrolStatus = testPatrolStatus;

            // Assert that ViewModel.CompletionPercentage equals 50.0
            viewModel.CompletionPercentage.Should().Be(50.0);
        }

        /// <summary>
        /// Tests that the StatusMessage property updates based on the current state
        /// </summary>
        [Fact]
        public void Test_StatusMessage_UpdatesBasedOnState()
        {
            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Assert that initial StatusMessage indicates to select a location
            viewModel.StatusMessage.Should().Be("Select a location to begin patrol");

            // Set ViewModel.IsLocationSelected to true
            viewModel.IsLocationSelected = true;

            // Assert that StatusMessage updates to indicate starting patrol
            viewModel.StatusMessage.Should().Be("Start patrol to begin verification");

            // Set ViewModel.IsPatrolActive to true
            viewModel.IsPatrolActive = true;

            // Assert that StatusMessage updates to indicate moving to checkpoints
            viewModel.StatusMessage.Should().Be("Move closer to a checkpoint to verify");

            // Set ViewModel.CanVerifyCheckpoint to true
            viewModel.CanVerifyCheckpoint = true;

            // Assert that StatusMessage updates to indicate checkpoint in range
            viewModel.StatusMessage.Should().Be("Checkpoint in range, tap to verify");
        }

        /// <summary>
        /// Tests that OnNavigatedFrom unsubscribes from events
        /// </summary>
        [Fact]
        public async Task Test_OnNavigatedFrom_UnsubscribesFromEvents()
        {
            // Create the ViewModel
            var viewModel = CreateViewModel();

            // Call OnNavigatedFrom
            await viewModel.OnNavigatedFrom();

            // Raise the CheckpointProximityChanged event on MockPatrolService
            _patrolService.CheckpointProximityChanged?.Invoke(this, new CheckpointProximityEventArgs(1, 10, true));

            // Raise the LocationChanged event on MockLocationService
            _locationService.LocationChanged?.Invoke(this, new LocationChangedEventArgs(new LocationModel()));

            // Verify that no interactions with MockMapService occurred
            MockMapService.VerifyNoOtherCalls();

            // Assert that ViewModel state remains unchanged
            viewModel.CanVerifyCheckpoint.Should().Be(false);
        }
    }
}