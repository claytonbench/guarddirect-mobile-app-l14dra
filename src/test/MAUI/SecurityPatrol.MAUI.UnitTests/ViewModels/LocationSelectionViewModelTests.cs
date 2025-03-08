using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq; // Version 4.18.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    /// <summary>
    /// Unit tests for the LocationSelectionViewModel class
    /// </summary>
    public class LocationSelectionViewModelTests : TestBase
    {
        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Arrange
            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            // Act
            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Assert
            viewModel.Locations.Should().NotBeNull();
            viewModel.Title.Should().Be("Select Location");
            viewModel.IsLoading.Should().BeFalse();
            viewModel.HasLocations.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeAsync_LoadsLocations_Success()
        {
            // Arrange
            var testLocations = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1, 34.0522, -118.2437),
                MockDataGenerator.CreateLocationModel(2, 34.0531, -118.2445)
            };

            MockPatrolService.Setup(x => x.GetLocations())
                .ReturnsAsync(testLocations);

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            viewModel.Locations.Count.Should().Be(2);
            viewModel.Locations.Should().BeEquivalentTo(testLocations);
            viewModel.HasLocations.Should().BeTrue();
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeAsync_LoadsLocations_EmptyList()
        {
            // Arrange
            MockPatrolService.Setup(x => x.GetLocations())
                .ReturnsAsync(new List<LocationModel>());

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            viewModel.Locations.Should().BeEmpty();
            viewModel.HasLocations.Should().BeFalse();
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeAsync_LoadsLocations_Exception()
        {
            // Arrange
            MockPatrolService.Setup(x => x.GetLocations())
                .ThrowsAsync(new Exception("Test exception"));

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            viewModel.Locations.Should().BeEmpty();
            viewModel.HasLocations.Should().BeFalse();
            viewModel.IsLoading.Should().BeFalse();
            viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task LoadLocationsAsync_Success()
        {
            // Arrange
            var testLocations = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1, 34.0522, -118.2437),
                MockDataGenerator.CreateLocationModel(2, 34.0531, -118.2445)
            };

            MockPatrolService.Setup(x => x.GetLocations())
                .ReturnsAsync(testLocations);

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await viewModel.LoadLocationsAsync();

            // Assert
            viewModel.Locations.Count.Should().Be(2);
            viewModel.Locations.Should().BeEquivalentTo(testLocations);
            viewModel.HasLocations.Should().BeTrue();
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task LoadLocationsAsync_EmptyList()
        {
            // Arrange
            MockPatrolService.Setup(x => x.GetLocations())
                .ReturnsAsync(new List<LocationModel>());

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await viewModel.LoadLocationsAsync();

            // Assert
            viewModel.Locations.Should().BeEmpty();
            viewModel.HasLocations.Should().BeFalse();
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task LoadLocationsAsync_Exception()
        {
            // Arrange
            MockPatrolService.Setup(x => x.GetLocations())
                .ThrowsAsync(new Exception("Test exception"));

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await viewModel.LoadLocationsAsync();

            // Assert
            viewModel.Locations.Should().BeEmpty();
            viewModel.HasLocations.Should().BeFalse();
            viewModel.IsLoading.Should().BeFalse();
            viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task SelectLocation_Success()
        {
            // Arrange
            var testLocation = MockDataGenerator.CreateLocationModel(1, 34.0522, -118.2437);

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await ((System.Windows.Input.ICommand)viewModel.SelectLocationCommand).ExecuteAsync(testLocation);

            // Assert
            viewModel.SelectedLocation.Should().Be(testLocation);
            
            MockNavigationService.Verify(x => x.NavigateToAsync(
                It.Is<string>(s => s == NavigationConstants.PatrolPage),
                It.Is<Dictionary<string, object>>(d => d.ContainsKey(NavigationConstants.ParamLocationId) && 
                                                 (int)d[NavigationConstants.ParamLocationId] == testLocation.Id)),
                Times.Once);
        }

        [Fact]
        public void SelectLocation_NullLocation()
        {
            // Arrange
            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            ((System.Windows.Input.ICommand)viewModel.SelectLocationCommand).Execute(null);

            // Assert
            MockNavigationService.Verify(x => x.NavigateToAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), 
                Times.Never);
        }

        [Fact]
        public void CanSelectLocation_WithValidLocation_ReturnsTrue()
        {
            // Arrange
            var testLocation = MockDataGenerator.CreateLocationModel(1, 34.0522, -118.2437);

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act & Assert
            viewModel.SelectLocationCommand.CanExecute(testLocation).Should().BeTrue();
        }

        [Fact]
        public void CanSelectLocation_WithNullLocation_ReturnsFalse()
        {
            // Arrange
            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act & Assert
            viewModel.SelectLocationCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public async Task RefreshLocationsCommand_ExecutesLoadLocationsAsync()
        {
            // Arrange
            var testLocations = new List<LocationModel>
            {
                MockDataGenerator.CreateLocationModel(1, 34.0522, -118.2437),
                MockDataGenerator.CreateLocationModel(2, 34.0531, -118.2445)
            };

            MockPatrolService.Setup(x => x.GetLocations())
                .ReturnsAsync(testLocations);

            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);

            // Act
            await ((System.Windows.Input.ICommand)viewModel.RefreshLocationsCommand).ExecuteAsync(null);

            // Assert
            viewModel.Locations.Count.Should().Be(2);
            viewModel.Locations.Should().BeEquivalentTo(testLocations);
            viewModel.HasLocations.Should().BeTrue();
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public void UpdateHasLocations_WithLocations_SetsHasLocationsTrue()
        {
            // Arrange
            var testLocation = MockDataGenerator.CreateLocationModel(1, 34.0522, -118.2437);
            
            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);
            
            // Act
            viewModel.Locations.Add(testLocation);
            // Trigger property changed event
            viewModel.OnPropertyChanged(nameof(viewModel.Locations));
            
            // Assert
            viewModel.HasLocations.Should().BeTrue();
        }

        [Fact]
        public void UpdateHasLocations_WithEmptyLocations_SetsHasLocationsFalse()
        {
            // Arrange
            var navigationService = MockNavigationService.Object;
            var authStateProvider = MockAuthenticationStateProvider.Object;
            var patrolService = MockPatrolService.Object;

            var viewModel = new LocationSelectionViewModel(navigationService, authStateProvider, patrolService);
            
            // Act
            viewModel.Locations.Clear();
            // Trigger property changed event
            viewModel.OnPropertyChanged(nameof(viewModel.Locations));
            
            // Assert
            viewModel.HasLocations.Should().BeFalse();
        }
    }
}