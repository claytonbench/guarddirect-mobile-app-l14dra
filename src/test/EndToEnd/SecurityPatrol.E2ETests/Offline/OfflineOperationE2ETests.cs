# src/test/EndToEnd/SecurityPatrol.E2ETests/Offline/OfflineOperationE2ETests.cs
using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.10.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Mocks; // MockNetworkService
using SecurityPatrol.TestCommon.Helpers; // NetworkConditionSimulator
using SecurityPatrol.Models; // ConnectionQuality
using SecurityPatrol.Helpers; // ConnectionQuality
using SecurityPatrol.TestCommon.Constants; // TestConstants

namespace SecurityPatrol.E2ETests.Offline
{
    /// <summary>
    /// Contains end-to-end tests for verifying the application's offline operation capabilities
    /// </summary>
    [public]
    public class OfflineOperationE2ETests : E2ETestBase
    {
        [private]
        private MockNetworkService _mockNetworkService;
        [private]
        private INetworkService _networkService;

        /// <summary>
        /// Initializes a new instance of the OfflineOperationE2ETests class
        /// </summary>
        public OfflineOperationE2ETests()
        {
            _mockNetworkService = new MockNetworkService();
        }

        /// <summary>
        /// Initializes the test environment before each test
        /// </summary>
        [public]
        [override]
        [async]
        public override async Task InitializeAsync()
        {
            // Call base.InitializeAsync() to initialize the base test environment
            await base.InitializeAsync();

            // Configure TestEnvironmentSetup to use _mockNetworkService
            TestEnvironmentSetup.ConfigureNetworkConnectivity(true);

            // Get _networkService from ServiceProvider
            _networkService = ServiceProvider.GetService<INetworkService>();

            // Ensure network is connected with high quality
            _mockNetworkService.SetNetworkConnected(true);
            _mockNetworkService.SetConnectionQuality(ConnectionQuality.High);
        }

        /// <summary>
        /// Cleans up the test environment after each test
        /// </summary>
        [public]
        [override]
        [async]
        public override async Task DisposeAsync()
        {
            // Reset network connectivity to connected state
            _mockNetworkService.SetNetworkConnected(true);

            // Call base.DisposeAsync() to clean up the base test environment
            await base.DisposeAsync();
        }

        /// <summary>
        /// Tests that clock in/out operations work while offline
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestClockInOutOffline()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Verify that network is disconnected
            _networkService.IsConnected.Should().BeFalse();

            // Perform clock in operation while offline
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Verify that clock in was successful and stored locally
            TimeTrackingService.LastClockInRecord.Should().NotBeNull();

            // Perform clock out operation while offline
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Verify that clock out was successful and stored locally
            TimeTrackingService.LastClockOutRecord.Should().NotBeNull();

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that clock events were successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests that location tracking works while offline
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestLocationTrackingOffline()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Perform clock in operation while online
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Verify that location tracking continues to collect data while offline
            _networkService.IsConnected.Should().BeFalse();

            // Wait for sufficient location points to be collected
            await Task.Delay(5000); // Wait 5 seconds

            // Verify that location data is stored locally
            LocationService.GetLocationHistory().Count.Should().BeGreaterThan(0);

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that location data was successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests that photo capturing works while offline
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPhotoCapturingOffline()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Capture multiple photos while offline
            for (int i = 0; i < 3; i++)
            {
                await CapturePhotoAsync();
            }

            // Verify that photos are stored locally
            PhotoService.Photos.Count.Should().Be(3);

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that photos were successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests that activity reporting works while offline
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestActivityReportingOffline()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Create multiple activity reports while offline
            for (int i = 0; i < 3; i++)
            {
                await CreateReportAsync($"Test report {i}");
            }

            // Verify that reports are stored locally
            ReportService.GetAllReportsAsync().Result.Should().HaveCount(3);

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that reports were successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests that patrol checkpoint verification works while offline
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestPatrolVerificationOffline()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Load patrol locations and checkpoints while online
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            var checkpoints = await PatrolService.GetCheckpoints(locations.First().Id);
            checkpoints.Should().NotBeNullOrEmpty();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Verify multiple checkpoints while offline
            foreach (var checkpoint in checkpoints.Take(2))
            {
                await PatrolService.VerifyCheckpoint(checkpoint.Id);
            }

            // Verify that checkpoint verifications are stored locally
            PatrolService.VerifyVerifyCheckpointCalled().Should().BeTrue();

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that checkpoint verifications were successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests a complete patrol flow with all operations performed offline
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestCompletePatrolFlowOffline()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Load patrol locations and checkpoints while online
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            var checkpoints = await PatrolService.GetCheckpoints(locations.First().Id);
            checkpoints.Should().NotBeNullOrEmpty();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Perform clock in operation while offline
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Verify multiple checkpoints while offline
            foreach (var checkpoint in checkpoints.Take(2))
            {
                await PatrolService.VerifyCheckpoint(checkpoint.Id);
            }

            // Capture photos of checkpoints while offline
            await CapturePhotoAsync();

            // Create activity reports while offline
            await CreateReportAsync("Test report");

            // Perform clock out operation while offline
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Verify that all data is stored locally
            TimeTrackingService.LastClockInRecord.Should().NotBeNull();
            TimeTrackingService.LastClockOutRecord.Should().NotBeNull();
            PhotoService.Photos.Should().NotBeNullOrEmpty();
            ReportService.GetAllReportsAsync().Result.Should().NotBeNullOrEmpty();
            PatrolService.VerifyVerifyCheckpointCalled().Should().BeTrue();

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that all data was successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests application behavior with intermittent network connectivity
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestOfflineOperationWithIntermittentConnectivity()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Perform a sequence of operations with changing network conditions:
            // - Clock in while online
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // - Simulate network loss
            SimulateNetworkLoss();

            // - Verify checkpoints while offline
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNullOrEmpty();

            var checkpoints = await PatrolService.GetCheckpoints(locations.First().Id);
            checkpoints.Should().NotBeNullOrEmpty();

            foreach (var checkpoint in checkpoints.Take(2))
            {
                await PatrolService.VerifyCheckpoint(checkpoint.Id);
            }

            // - Simulate network restoration with low quality
            SimulateNetworkRestoration(ConnectionQuality.Low);

            // - Capture photos with poor connectivity
            await CapturePhotoAsync();

            // - Simulate network loss again
            SimulateNetworkLoss();

            // - Create reports while offline
            await CreateReportAsync("Test report");

            // - Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // - Clock out with good connectivity
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Verify that all operations were completed successfully
            TimeTrackingService.LastClockInRecord.Should().NotBeNull();
            TimeTrackingService.LastClockOutRecord.Should().NotBeNull();
            PhotoService.Photos.Should().NotBeNullOrEmpty();
            ReportService.GetAllReportsAsync().Result.Should().NotBeNullOrEmpty();
            PatrolService.VerifyVerifyCheckpointCalled().Should().BeTrue();

            // Verify that all data was eventually synchronized
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Tests that offline data persists across application restarts
        /// </summary>
        [public]
        [async]
        [Fact]
        public async Task TestOfflineDataPersistenceAcrossAppRestarts()
        {
            // Authenticate the user while online
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Simulate network loss using NetworkConditionSimulator
            SimulateNetworkLoss();

            // Perform various operations while offline (clock in, photos, reports)
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();
            await CapturePhotoAsync();
            await CreateReportAsync("Test report");

            // Simulate application restart by recreating services
            await SimulateAppRestart();

            // Verify that all offline data is still available after restart
            TimeTrackingService.LastClockInRecord.Should().NotBeNull();
            PhotoService.Photos.Should().NotBeNullOrEmpty();
            ReportService.GetAllReportsAsync().Result.Should().NotBeNullOrEmpty();

            // Simulate network restoration with high quality
            SimulateNetworkRestoration(ConnectionQuality.High);

            // Synchronize data with backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Verify that all data was successfully synchronized
            (await VerifyAllDataSynced()).Should().BeTrue();
        }

        /// <summary>
        /// Helper method to simulate network connectivity loss
        /// </summary>
        [private]
        private void SimulateNetworkLoss()
        {
            // Call NetworkConditionSimulator.SimulateNetworkLoss(_mockNetworkService)
            NetworkConditionSimulator.SimulateNetworkLoss(_mockNetworkService);

            // Verify that _networkService.IsConnected is false
            _networkService.IsConnected.Should().BeFalse();
        }

        /// <summary>
        /// Helper method to simulate network connectivity restoration
        /// </summary>
        /// <param name="quality">ConnectionQuality: quality</param>
        [private]
        private void SimulateNetworkRestoration(ConnectionQuality quality)
        {
            // Call NetworkConditionSimulator.SimulateNetworkRestoration(_mockNetworkService, quality)
            NetworkConditionSimulator.SimulateNetworkRestoration(_mockNetworkService, quality);

            // Verify that _networkService.IsConnected is true
            _networkService.IsConnected.Should().BeTrue();
        }

        /// <summary>
        /// Helper method to verify synchronization status
        /// </summary>
        [private]
        [async]
        private async Task<Dictionary<string, int>> VerifySyncStatus()
        {
            // Call SyncService.GetSyncStatus() to get current sync status
            var syncStatus = await SyncService.GetSyncStatus();

            // Return the dictionary of pending items by entity type
            return syncStatus;
        }

        /// <summary>
        /// Helper method to verify that all data has been synchronized
        /// </summary>
        [private]
        [async]
        private async Task<bool> VerifyAllDataSynced()
        {
            // Call VerifySyncStatus() to get current sync status
            var syncStatus = await VerifySyncStatus();

            // Check if all entity types have zero pending items
            bool allSynced = syncStatus.Values.All(count => count == 0);

            // Return true if all synchronized, false otherwise
            return allSynced;
        }

        /// <summary>
        /// Helper method to simulate application restart
        /// </summary>
        [private]
        [async]
        private async Task SimulateAppRestart()
        {
            // Dispose current service provider
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            // Create a new service provider
            var services = new ServiceCollection();
            RegisterServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Reinitialize service references
            await InitializeServicesAsync();

            // Ensure network connectivity state is preserved
            TestEnvironmentSetup.ConfigureNetworkConnectivity(_networkService.IsConnected);
        }
    }
}