# src/test/EndToEnd/SecurityPatrol.E2ETests/Offline/SyncRecoveryE2ETests.cs
using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Helpers; // NetworkConditionSimulator
using SecurityPatrol.TestCommon.Mocks; // MockNetworkService
using SecurityPatrol.TestCommon.Mocks; // MockSyncService
using SecurityPatrol.Models; // ConnectionQuality

namespace SecurityPatrol.E2ETests.Offline
{
    /// <summary>
    /// End-to-end tests that verify the application's ability to recover and synchronize data after network connectivity is restored.
    /// </summary>
    [Collection("E2E Tests")]
    public class SyncRecoveryE2ETests : E2ETestBase
    {
        private MockNetworkService _networkService;
        private MockSyncService _syncService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncRecoveryE2ETests"/> class.
        /// </summary>
        public SyncRecoveryE2ETests()
        {
        }

        /// <summary>
        /// Initializes the test environment and retrieves the mock services needed for testing.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _networkService = ServiceProvider.GetService<MockNetworkService>();
            _syncService = ServiceProvider.GetService<MockSyncService>();

            _networkService.Should().NotBeNull("MockNetworkService should be registered");
            _syncService.Should().NotBeNull("MockSyncService should be registered");

            _networkService.SetNetworkConnected(true);
        }

        /// <summary>
        /// Cleans up the test environment after each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task DisposeAsync()
        {
            _networkService.SetNetworkConnected(true);
            await base.DisposeAsync();
        }

        /// <summary>
        /// Tests that the application can recover and synchronize data after a simple network outage.
        /// </summary>
        [Fact]
        public async Task TestBasicSyncRecoveryAsync()
        {
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            NetworkConditionSimulator.SimulateNetworkLoss(_networkService);

            bool photoCaptured = await CapturePhotoAsync();
            photoCaptured.Should().BeTrue("Photo capture should succeed offline");

            bool reportCreated = await CreateReportAsync("Test report");
            reportCreated.Should().BeTrue("Report creation should succeed offline");

            bool checkpointVerified = await VerifyCheckpointAsync(101);
            checkpointVerified.Should().BeTrue("Checkpoint verification should succeed offline");

            _syncService.SyncAllCallCount.Should().Be(0, "Sync should not be attempted while offline");

            NetworkConditionSimulator.SimulateNetworkRestoration(_networkService, ConnectionQuality.High);

            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Sync should succeed after network restoration");
            _syncService.SyncAllCallCount.Should().Be(1, "Sync should be attempted once after network restoration");

            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");
        }

        /// <summary>
        /// Tests that the application can handle intermittent connectivity and still synchronize data successfully.
        /// </summary>
        [Fact]
        public async Task TestIntermittentConnectivitySyncAsync()
        {
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            _syncService.SetPendingItems("Photo", 3);
            _syncService.SetPendingItems("Report", 2);
            _syncService.SetPendingItems("Checkpoint", 1);

            await NetworkConditionSimulator.SimulateIntermittentConnectivity(_networkService, 3, 2000, 1000);

            _syncService.SyncAllCallCount.Should().BeGreaterThan(0, "Sync should be attempted during intermittent connectivity");

            NetworkConditionSimulator.SimulateNetworkRestoration(_networkService, ConnectionQuality.High);

            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Final sync should succeed");

            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");
        }

        /// <summary>
        /// Tests that the application correctly retries failed synchronization operations.
        /// </summary>
        [Fact]
        public async Task TestSyncRetryAfterFailureAsync()
        {
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            _syncService.ShouldSucceed = false;

            bool photoCaptured = await CapturePhotoAsync();
            photoCaptured.Should().BeTrue("Photo capture should succeed");

            bool reportCreated = await CreateReportAsync("Test report");
            reportCreated.Should().BeTrue("Report creation should succeed");

            bool checkpointVerified = await VerifyCheckpointAsync(101);
            checkpointVerified.Should().BeTrue("Checkpoint verification should succeed");

            bool syncAttempt1 = await SyncDataAsync();
            syncAttempt1.Should().BeFalse("Sync should fail initially");
            _syncService.SyncAllCallCount.Should().Be(1, "Sync should be attempted");

            _syncService.ShouldSucceed = true;

            bool syncAttempt2 = await SyncDataAsync();
            syncAttempt2.Should().BeTrue("Sync should succeed on retry");

            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");
        }

        /// <summary>
        /// Tests a complete patrol workflow with a network outage in the middle.
        /// </summary>
        [Fact]
        public async Task TestCompleteWorkflowWithNetworkOutageAsync()
        {
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            bool photoCaptured1 = await CapturePhotoAsync();
            photoCaptured1.Should().BeTrue("Photo capture should succeed");

            NetworkConditionSimulator.SimulateNetworkLoss(_networkService);

            bool reportCreated = await CreateReportAsync("Test report");
            reportCreated.Should().BeTrue("Report creation should succeed offline");

            bool checkpointVerified = await VerifyCheckpointAsync(101);
            checkpointVerified.Should().BeTrue("Checkpoint verification should succeed offline");

            NetworkConditionSimulator.SimulateNetworkRestoration(_networkService, ConnectionQuality.High);

            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Sync should succeed after network restoration");

            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");
        }

        /// <summary>
        /// Tests recovery after an extended network outage with multiple operations.
        /// </summary>
        [Fact]
        public async Task TestLongNetworkOutageRecoveryAsync()
        {
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue("Authentication should succeed");

            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue("Clock-in should succeed");

            NetworkConditionSimulator.SimulateNetworkLoss(_networkService);

            for (int i = 0; i < 5; i++)
            {
                bool photoCaptured = await CapturePhotoAsync();
                photoCaptured.Should().BeTrue($"Photo capture {i + 1} should succeed offline");

                bool reportCreated = await CreateReportAsync($"Test report {i + 1}");
                reportCreated.Should().BeTrue($"Report creation {i + 1} should succeed offline");

                bool checkpointVerified = await VerifyCheckpointAsync(101 + i);
                checkpointVerified.Should().BeTrue($"Checkpoint verification {i + 1} should succeed offline");
            }

            NetworkConditionSimulator.SimulateNetworkRestoration(_networkService, ConnectionQuality.High);

            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue("Sync should succeed after network restoration");

            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue("Clock-out should succeed");
        }
    }
}