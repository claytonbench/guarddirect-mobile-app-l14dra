using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.UnitTests.Helpers.MockServices;

namespace SecurityPatrol.UnitTests.ViewModels
{
    /// <summary>
    /// Test class for PatrolViewModel that verifies its functionality for managing patrol operations, 
    /// checkpoint visualization, and verification
    /// </summary>
    public class PatrolViewModelTests : IDisposable
    {
        private readonly MockPatrolService patrolService;
        private readonly MockLocationService locationService;
        private readonly Mock<IMapService> mockMapService;
        private readonly Mock<INavigationService> mockNavigationService;
        private readonly Mock<IAuthenticationStateProvider> mockAuthStateProvider;
        private readonly PatrolViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the PatrolViewModelTests class with mock services
        /// </summary>
        public PatrolViewModelTests()
        {
            // Set up mock services
            patrolService = new MockPatrolService();
            locationService = new MockLocationService();
            mockMapService = new Mock<IMapService>();
            mockNavigationService = new Mock<INavigationService>();
            mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();

            // Set up auth state
            var authState = TestDataGenerator.CreateAuthState(true);
            mockAuthStateProvider.Setup(a => a.GetCurrentState()).ReturnsAsync(authState);
            mockAuthStateProvider.Setup(a => a.IsAuthenticated()).ReturnsAsync(true);

            // Create the view model with mock services
            viewModel = new PatrolViewModel(
                mockNavigationService.Object,
                mockAuthStateProvider.Object,
                patrolService,
                mockMapService.Object,
                locationService);
        }

        /// <summary>
        /// Cleans up resources after tests
        /// </summary>
        public void Dispose()
        {
            // Clean up
            viewModel?.Dispose();
            patrolService.Reset();
            locationService.Reset();
        }

        /// <summary>
        /// Tests that InitializeAsync loads locations from the patrol service
        /// </summary>
        [Fact]
        public async Task InitializeViewModel_ShouldLoadLocations()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(3);
            patrolService.SetupLocations(testLocations);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            Assert.True(patrolService.VerifyGetLocationsCalled());
            Assert.Equal(testLocations.Count, viewModel.Locations.Count);
            Assert.False(viewModel.IsBusy);
            
            // Verify locations are properly loaded
            for (int i = 0; i < testLocations.Count; i++)
            {
                Assert.Equal(testLocations[i].Id, viewModel.Locations[i].Id);
                Assert.Equal(testLocations[i].Name, viewModel.Locations[i].Name);
            }
        }

        /// <summary>
        /// Tests that InitializeAsync loads patrol status when a patrol is active
        /// </summary>
        [Fact]
        public async Task InitializeViewModel_WhenPatrolActive_ShouldLoadPatrolStatus()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(3);
            patrolService.SetupLocations(testLocations);

            // Configure patrol service to indicate active patrol
            patrolService.SetupPatrolStatus(TestDataGenerator.CreatePatrolStatus(
                locationId: testLocations[0].Id,
                totalCheckpoints: 5,
                verifiedCheckpoints: 2));
            
            // Set the first location as the active patrol location
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, testLocations[0].Id);
            patrolService.SetupCheckpoints(testLocations[0].Id, testCheckpoints);
            
            // Mock that a patrol is active at location 1
            var field = typeof(MockPatrolService).GetField("_isPatrolActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(patrolService, true);
            
            var locationIdField = typeof(MockPatrolService).GetField("_currentLocationId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            locationIdField.SetValue(patrolService, testLocations[0].Id);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            Assert.True(patrolService.VerifyGetLocationsCalled());
            Assert.True(viewModel.IsPatrolActive);
            Assert.Equal(testLocations[0].Id, viewModel.SelectedLocation.Id);
            Assert.True(viewModel.IsLocationSelected);
            Assert.Equal(5, viewModel.Checkpoints.Count);
            Assert.Equal(2, viewModel.CurrentPatrolStatus.VerifiedCheckpoints);
            Assert.Equal(5, viewModel.CurrentPatrolStatus.TotalCheckpoints);
            Assert.Equal(40, viewModel.CompletionPercentage); // 2/5 = 40%
        }

        /// <summary>
        /// Tests that OnNavigatedTo selects a location when a location ID is provided
        /// </summary>
        [Fact]
        public async Task OnNavigatedTo_WithLocationId_ShouldSelectLocation()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(3);
            patrolService.SetupLocations(testLocations);
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, testLocations[0].Id);
            patrolService.SetupCheckpoints(testLocations[0].Id, testCheckpoints);

            // Initialize the view model to load locations
            await viewModel.InitializeAsync();

            // Create parameters with a location ID
            var parameters = new Dictionary<string, object>
            {
                { NavigationConstants.ParamLocationId, testLocations[0].Id }
            };

            // Act
            await viewModel.OnNavigatedTo(parameters);

            // Assert
            Assert.True(patrolService.VerifyGetCheckpointsCalled());
            Assert.Equal(testLocations[0].Id, viewModel.SelectedLocation.Id);
            Assert.True(viewModel.IsLocationSelected);
            Assert.Equal(5, viewModel.Checkpoints.Count);
        }

        /// <summary>
        /// Tests that SelectLocationCommand selects a location and loads its checkpoints
        /// </summary>
        [Fact]
        public async Task SelectLocationCommand_ShouldSelectLocationAndLoadCheckpoints()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(3);
            patrolService.SetupLocations(testLocations);
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, testLocations[0].Id);
            patrolService.SetupCheckpoints(testLocations[0].Id, testCheckpoints);

            // Set up current location
            var currentLocation = TestDataGenerator.CreateLocationModel();
            locationService.SetupCurrentLocation(currentLocation);

            // Initialize the view model to load locations
            await viewModel.InitializeAsync();

            // Act
            await viewModel.SelectLocationCommand.ExecuteAsync(testLocations[0]);

            // Assert
            Assert.True(patrolService.VerifyGetCheckpointsCalled());
            Assert.True(locationService.VerifyGetCurrentLocationCalled());
            Assert.Equal(testLocations[0].Id, viewModel.SelectedLocation.Id);
            Assert.True(viewModel.IsLocationSelected);
            Assert.Equal(5, viewModel.Checkpoints.Count);
            
            // Verify map service was called to display checkpoints
            mockMapService.Verify(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()), Times.Once);
        }

        /// <summary>
        /// Tests that StartPatrolCommand starts a patrol for the selected location
        /// </summary>
        [Fact]
        public async Task StartPatrolCommand_ShouldStartPatrolForSelectedLocation()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(3);
            patrolService.SetupLocations(testLocations);
            
            var patrolStatus = TestDataGenerator.CreatePatrolStatus(
                locationId: testLocations[0].Id,
                totalCheckpoints: 5,
                verifiedCheckpoints: 0);
            patrolService.SetupPatrolStatus(patrolStatus);

            // Initialize the view model to load locations
            await viewModel.InitializeAsync();

            // Select a location
            viewModel.SelectedLocation = testLocations[0];

            // Act
            await viewModel.StartPatrolCommand.ExecuteAsync();

            // Assert
            Assert.True(patrolService.VerifyStartPatrolCalled());
            Assert.True(viewModel.IsPatrolActive);
            Assert.NotNull(viewModel.CurrentPatrolStatus);
            Assert.Equal(testLocations[0].Id, viewModel.CurrentPatrolStatus.LocationId);
            Assert.Equal(0, viewModel.CompletionPercentage); // 0/5 = 0%
        }

        /// <summary>
        /// Tests that EndPatrolCommand ends the active patrol
        /// </summary>
        [Fact]
        public async Task EndPatrolCommand_ShouldEndActivePatrol()
        {
            // Arrange
            var field = typeof(MockPatrolService).GetField("_isPatrolActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(patrolService, true);
            
            var patrolStatus = TestDataGenerator.CreatePatrolStatus(
                locationId: 1,
                totalCheckpoints: 5,
                verifiedCheckpoints: 3);
            patrolService.SetupPatrolStatus(patrolStatus);

            // Set up the view model state
            viewModel.IsPatrolActive = true;
            
            // Act
            await viewModel.EndPatrolCommand.ExecuteAsync();

            // Assert
            Assert.True(patrolService.VerifyEndPatrolCalled());
            Assert.False(viewModel.IsPatrolActive);
        }

        /// <summary>
        /// Tests that VerifyCheckpointCommand verifies the selected checkpoint
        /// </summary>
        [Fact]
        public async Task VerifyCheckpointCommand_ShouldVerifySelectedCheckpoint()
        {
            // Arrange
            var field = typeof(MockPatrolService).GetField("_isPatrolActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(patrolService, true);
            
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, 1, false);
            patrolService.SetupCheckpoints(1, testCheckpoints);
            patrolService.SetupVerifyCheckpointResult(true);
            
            var patrolStatus = TestDataGenerator.CreatePatrolStatus(
                locationId: 1,
                totalCheckpoints: 5,
                verifiedCheckpoints: 0);
            patrolService.SetupPatrolStatus(patrolStatus);

            // Initialize view model state
            viewModel.SelectedCheckpoint = testCheckpoints[0];
            viewModel.CanVerifyCheckpoint = true; // Enable verification
            viewModel.IsPatrolActive = true;

            // Act
            await viewModel.VerifyCheckpointCommand.ExecuteAsync(testCheckpoints[0]);

            // Assert
            Assert.True(patrolService.VerifyVerifyCheckpointCalled());
            Assert.Contains(testCheckpoints[0].Id, patrolService.GetVerifiedCheckpointIds());
        }

        /// <summary>
        /// Tests that RefreshCommand reloads locations and checkpoints
        /// </summary>
        [Fact]
        public async Task RefreshCommand_ShouldReloadLocationsAndCheckpoints()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(3);
            patrolService.SetupLocations(testLocations);
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, testLocations[0].Id);
            patrolService.SetupCheckpoints(testLocations[0].Id, testCheckpoints);

            // Initialize the view model to load initial data
            await viewModel.InitializeAsync();
            patrolService.GetLocationsCallCount = 0; // Reset call count
            
            // Select a location
            viewModel.SelectedLocation = testLocations[0];

            // Update test data to simulate changes
            var updatedLocations = TestDataGenerator.CreateLocationModels(4);
            patrolService.SetupLocations(updatedLocations);
            var updatedCheckpoints = TestDataGenerator.CreateCheckpointModels(6, testLocations[0].Id);
            patrolService.SetupCheckpoints(testLocations[0].Id, updatedCheckpoints);

            // Act
            await viewModel.RefreshCommand.ExecuteAsync();

            // Assert
            Assert.True(patrolService.VerifyGetLocationsCalled());
            Assert.Equal(4, viewModel.Locations.Count);
        }

        /// <summary>
        /// Tests that checkpoint proximity changes update the CanVerifyCheckpoint property
        /// </summary>
        [Fact]
        public async Task HandleCheckpointProximityChanged_ShouldUpdateCanVerifyCheckpoint()
        {
            // Arrange
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, 1, false);
            patrolService.SetupCheckpoints(1, testCheckpoints);
            
            // Initialize the view model
            await viewModel.InitializeAsync();
            
            // Set patrol active
            viewModel.IsPatrolActive = true;

            // Act - simulate checkpoint in range
            patrolService.SimulateCheckpointProximityChanged(
                testCheckpoints[0].Id, 
                40, // 40 feet distance
                true // in range
            );

            // Assert
            Assert.True(viewModel.CanVerifyCheckpoint);
            Assert.Equal(testCheckpoints[0].Id, viewModel.SelectedCheckpoint.Id);

            // Act - simulate checkpoint out of range
            patrolService.SimulateCheckpointProximityChanged(
                testCheckpoints[0].Id,
                60, // 60 feet distance
                false // out of range
            );

            // Assert
            Assert.False(viewModel.CanVerifyCheckpoint);
        }

        /// <summary>
        /// Tests that location changes update the location data
        /// </summary>
        [Fact]
        public async Task HandleLocationChanged_ShouldUpdateLocation()
        {
            // Arrange
            await viewModel.InitializeAsync();
            var newLocation = TestDataGenerator.CreateLocationModel(
                latitude: 35.6895,
                longitude: 139.6917
            );

            // Act
            locationService.SimulateLocationChanged(newLocation);

            // Assert
            mockMapService.Verify(m => m.UpdateUserLocation(
                It.Is<double>(lat => Math.Abs(lat - 35.6895) < 0.0001),
                It.Is<double>(lon => Math.Abs(lon - 139.6917) < 0.0001)
            ), Times.Once);
        }

        /// <summary>
        /// Tests that InitializeAsync handles exceptions gracefully
        /// </summary>
        [Fact]
        public async Task InitializeAsync_WithException_ShouldHandleError()
        {
            // Arrange
            patrolService.SetupException(new Exception("Test error"));

            // Act
            await viewModel.InitializeAsync();

            // Assert
            Assert.True(viewModel.HasError);
            Assert.Contains("Test error", viewModel.ErrorMessage);
            Assert.False(viewModel.IsBusy);
        }

        /// <summary>
        /// Tests that OnNavigatedFrom unsubscribes from events
        /// </summary>
        [Fact]
        public async Task OnNavigatedFrom_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            await viewModel.InitializeAsync();
            
            // Act
            await viewModel.OnNavigatedFrom();
            
            // Simulate events that should no longer be handled
            patrolService.SimulateCheckpointProximityChanged(1, 40, true);
            locationService.SimulateLocationChanged(TestDataGenerator.CreateLocationModel());
            
            // Assert - if handlers were unsubscribed, state shouldn't change
            Assert.False(viewModel.CanVerifyCheckpoint);
        }
    }
}