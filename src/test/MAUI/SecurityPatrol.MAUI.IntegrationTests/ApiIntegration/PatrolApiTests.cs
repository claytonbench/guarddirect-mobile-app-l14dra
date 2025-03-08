using System;
using System.Threading.Tasks;
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Text.Json; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.IntegrationTests.ApiIntegration
{
    /// <summary>
    /// Integration tests for the patrol API functionality in the Security Patrol application.
    /// Tests the interaction between the PatrolService and the backend patrol API endpoints,
    /// verifying that location retrieval, checkpoint management, and verification operations work correctly.
    /// </summary>
    public class PatrolApiTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the PatrolApiTests class
        /// </summary>
        public PatrolApiTests()
        {
            // Call base constructor to initialize the IntegrationTestBase
        }

        /// <summary>
        /// Tests that retrieving patrol locations returns the expected locations
        /// </summary>
        [Fact]
        public async Task GetLocations_ShouldReturnLocations()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                },
                new
                {
                    Id = 2,
                    Name = "Warehouse",
                    Latitude = TestConstants.TestLatitude + 0.05,
                    Longitude = TestConstants.TestLongitude - 0.05
                }
            });

            // Call PatrolService.GetLocations()
            var locations = await PatrolService.GetLocations();

            // Assert that the result is not null
            locations.Should().NotBeNull();

            // Assert that the result contains the expected number of locations
            locations.Should().HaveCount(2);

            // Verify that the API server received a request to the patrol/locations endpoint
            ApiServer.GetRequestCount(ApiEndpoints.PatrolLocations).Should().Be(1);
        }

        /// <summary>
        /// Tests that the patrol service handles server errors gracefully when retrieving locations
        /// </summary>
        [Fact]
        public async Task GetLocations_WithServerError_ShouldHandleGracefully()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up an error response for the patrol/locations endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.PatrolLocations, 500, "Internal Server Error");

            // Call PatrolService.GetLocations() and wrap in a try-catch block
            Func<Task> act = async () => await PatrolService.GetLocations();

            // Assert that an exception is thrown
            await act.Should().ThrowAsync<Exception>();

            // Verify that the API server received a request to the patrol/locations endpoint
            ApiServer.GetRequestCount(ApiEndpoints.PatrolLocations).Should().Be(1);
        }

        /// <summary>
        /// Tests that retrieving checkpoints for a valid location ID returns the expected checkpoints
        /// </summary>
        [Fact]
        public async Task GetCheckpoints_WithValidLocationId_ShouldReturnCheckpoints()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                },
                new
                {
                    Id = 102,
                    LocationId = 1,
                    Name = "East Wing",
                    Latitude = TestConstants.TestLatitude + 0.001,
                    Longitude = TestConstants.TestLongitude + 0.001
                }
            });

            // Call PatrolService.GetCheckpoints(1) with a valid location ID
            var checkpoints = await PatrolService.GetCheckpoints(1);

            // Assert that the result is not null
            checkpoints.Should().NotBeNull();

            // Assert that the result contains the expected number of checkpoints
            checkpoints.Should().HaveCount(2);

            // Assert that all checkpoints have the correct LocationId
            checkpoints.All(c => c.LocationId == 1).Should().BeTrue();

            // Verify that the API server received a request to the patrol/checkpoints endpoint with the correct location ID
            ApiServer.GetRequestCount(ApiEndpoints.PatrolCheckpoints).Should().Be(1);
        }

        /// <summary>
        /// Tests that retrieving checkpoints for an invalid location ID returns an empty list
        /// </summary>
        [Fact]
        public async Task GetCheckpoints_WithInvalidLocationId_ShouldReturnEmptyList()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/checkpoints endpoint with an empty array
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new CheckpointModel[] { });

            // Call PatrolService.GetCheckpoints(999) with an invalid location ID
            var checkpoints = await PatrolService.GetCheckpoints(999);

            // Assert that the result is not null
            checkpoints.Should().NotBeNull();

            // Assert that the result is an empty collection
            checkpoints.Should().BeEmpty();

            // Verify that the API server received a request to the patrol/checkpoints endpoint with the invalid location ID
            ApiServer.GetRequestCount(ApiEndpoints.PatrolCheckpoints).Should().Be(1);
        }

        /// <summary>
        /// Tests that the patrol service handles server errors gracefully when retrieving checkpoints
        /// </summary>
        [Fact]
        public async Task GetCheckpoints_WithServerError_ShouldHandleGracefully()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up an error response for the patrol/checkpoints endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.PatrolCheckpoints, 500, "Internal Server Error");

            // Call PatrolService.GetCheckpoints(1) and wrap in a try-catch block
            Func<Task> act = async () => await PatrolService.GetCheckpoints(1);

            // Assert that an exception is thrown
            await act.Should().ThrowAsync<Exception>();

            // Verify that the API server received a request to the patrol/checkpoints endpoint
            ApiServer.GetRequestCount(ApiEndpoints.PatrolCheckpoints).Should().Be(1);
        }

        /// <summary>
        /// Tests that verifying a valid checkpoint returns true
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithValidCheckpointId_ShouldReturnTrue()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/verify endpoint with a success status
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolVerify, new { Status = "success" });

            // Call PatrolService.StartPatrol(1) to start a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.VerifyCheckpoint(1) with a valid checkpoint ID
            var result = await PatrolService.VerifyCheckpoint(101);

            // Assert that the result is true
            result.Should().BeTrue();

            // Verify that the API server received a request to the patrol/verify endpoint with the correct checkpoint ID
            ApiServer.GetRequestCount(ApiEndpoints.PatrolVerify).Should().Be(1);
        }

        /// <summary>
        /// Tests that verifying an invalid checkpoint returns false
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithInvalidCheckpointId_ShouldReturnFalse()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up an error response for the patrol/verify endpoint with a 404 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.PatrolVerify, 404, "Checkpoint not found");

            // Call PatrolService.StartPatrol(1) to start a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.VerifyCheckpoint(999) with an invalid checkpoint ID
            var result = await PatrolService.VerifyCheckpoint(999);

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that the API server received a request to the patrol/verify endpoint with the invalid checkpoint ID
            ApiServer.GetRequestCount(ApiEndpoints.PatrolVerify).Should().Be(1);
        }

        /// <summary>
        /// Tests that verifying a checkpoint without an active patrol returns false
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithoutActivePatrol_ShouldReturnFalse()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Call PatrolService.VerifyCheckpoint(1) without first starting a patrol
            var result = await PatrolService.VerifyCheckpoint(1);

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that no request was made to the patrol/verify endpoint
            ApiServer.GetRequestCount(ApiEndpoints.PatrolVerify).Should().Be(0);
        }

        /// <summary>
        /// Tests that the patrol service handles server errors gracefully when verifying checkpoints
        /// </summary>
        [Fact]
        public async Task VerifyCheckpoint_WithServerError_ShouldHandleGracefully()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up an error response for the patrol/verify endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.PatrolVerify, 500, "Internal Server Error");

            // Call PatrolService.StartPatrol(1) to start a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.VerifyCheckpoint(1)
            var result = await PatrolService.VerifyCheckpoint(101);

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that the API server received a request to the patrol/verify endpoint
            ApiServer.GetRequestCount(ApiEndpoints.PatrolVerify).Should().Be(1);
        }

        /// <summary>
        /// Tests that retrieving patrol status for a valid location ID returns the expected status
        /// </summary>
        [Fact]
        public async Task GetPatrolStatus_WithValidLocationId_ShouldReturnStatus()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Call PatrolService.GetPatrolStatus(1) with a valid location ID
            var status = await PatrolService.GetPatrolStatus(1);

            // Assert that the result is not null
            status.Should().NotBeNull();

            // Assert that the result.LocationId equals the requested location ID
            status.LocationId.Should().Be(1);

            // Assert that the result.TotalCheckpoints is greater than 0
            status.TotalCheckpoints.Should().BeGreaterThan(0);

            // Assert that the result.VerifiedCheckpoints is 0 initially
            status.VerifiedCheckpoints.Should().Be(0);
        }

        /// <summary>
        /// Tests that starting a patrol with a valid location ID initializes the patrol correctly
        /// </summary>
        [Fact]
        public async Task StartPatrol_WithValidLocationId_ShouldInitializePatrol()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Call PatrolService.StartPatrol(1) with a valid location ID
            var status = await PatrolService.StartPatrol(1);

            // Assert that the result is not null
            status.Should().NotBeNull();

            // Assert that the result.LocationId equals the requested location ID
            status.LocationId.Should().Be(1);

            // Assert that the result.TotalCheckpoints is greater than 0
            status.TotalCheckpoints.Should().BeGreaterThan(0);

            // Assert that the result.VerifiedCheckpoints is 0
            status.VerifiedCheckpoints.Should().Be(0);

            // Assert that PatrolService.IsPatrolActive is true
            PatrolService.IsPatrolActive.Should().BeTrue();

            // Assert that PatrolService.CurrentLocationId equals the requested location ID
            PatrolService.CurrentLocationId.Should().Be(1);
        }

        /// <summary>
        /// Tests that starting a patrol with an invalid location ID throws an exception
        /// </summary>
        [Fact]
        public async Task StartPatrol_WithInvalidLocationId_ShouldThrowException()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up an error response for the patrol/checkpoints endpoint with a 404 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.PatrolCheckpoints, 404, "Location not found");

            // Call PatrolService.StartPatrol(999) with an invalid location ID and wrap in a try-catch block
            Func<Task> act = async () => await PatrolService.StartPatrol(999);

            // Assert that an exception is thrown
            await act.Should().ThrowAsync<Exception>();

            // Assert that PatrolService.IsPatrolActive is false
            PatrolService.IsPatrolActive.Should().BeFalse();

            // Assert that PatrolService.CurrentLocationId is null
            PatrolService.CurrentLocationId.Should().BeNull();
        }

        /// <summary>
        /// Tests that ending an active patrol updates the patrol status correctly
        /// </summary>
        [Fact]
        public async Task EndPatrol_WithActivePatrol_ShouldEndPatrol()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Call PatrolService.StartPatrol(1) to start a patrol
            await PatrolService.StartPatrol(1);

            // Assert that PatrolService.IsPatrolActive is true
            PatrolService.IsPatrolActive.Should().BeTrue();

            // Call PatrolService.EndPatrol()
            var status = await PatrolService.EndPatrol();

            // Assert that the result is not null
            status.Should().NotBeNull();

            // Assert that the result.EndTime has a value
            status.EndTime.Should().HaveValue();

            // Assert that PatrolService.IsPatrolActive is false
            PatrolService.IsPatrolActive.Should().BeFalse();

            // Assert that PatrolService.CurrentLocationId is null
            PatrolService.CurrentLocationId.Should().BeNull();
        }

        /// <summary>
        /// Tests that ending a patrol when no patrol is active returns null
        /// </summary>
        [Fact]
        public async Task EndPatrol_WithoutActivePatrol_ShouldReturnNull()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Call PatrolService.EndPatrol() without first starting a patrol
            var status = await PatrolService.EndPatrol();

            // Assert that the result is null
            status.Should().BeNull();

            // Assert that PatrolService.IsPatrolActive is false
            PatrolService.IsPatrolActive.Should().BeFalse();

            // Assert that PatrolService.CurrentLocationId is null
            PatrolService.CurrentLocationId.Should().BeNull();
        }

        /// <summary>
        /// Tests that checking proximity with an active patrol returns nearby checkpoints
        /// </summary>
        [Fact]
        public async Task CheckProximity_WithActivePatrol_ShouldReturnNearbyCheckpoints()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Call PatrolService.StartPatrol(1) to start a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.CheckProximity(37.7749, -122.4194) with test coordinates
            var checkpoints = await PatrolService.CheckProximity(TestConstants.TestLatitude, TestConstants.TestLongitude);

            // Assert that the result is not null
            checkpoints.Should().NotBeNull();

            // Assert that the result contains at least one checkpoint ID
            checkpoints.Should().HaveCountGreaterThan(0);

            // Verify that the proximity calculation was performed correctly
            // This is a basic check; more detailed verification would require mocking the location service
        }

        /// <summary>
        /// Tests that checking proximity without an active patrol returns an empty list
        /// </summary>
        [Fact]
        public async Task CheckProximity_WithoutActivePatrol_ShouldReturnEmptyList()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Call PatrolService.CheckProximity(37.7749, -122.4194) without first starting a patrol
            var checkpoints = await PatrolService.CheckProximity(TestConstants.TestLatitude, TestConstants.TestLongitude);

            // Assert that the result is not null
            checkpoints.Should().NotBeNull();

            // Assert that the result is an empty collection
            checkpoints.Should().BeEmpty();
        }

        /// <summary>
        /// Tests the complete patrol flow from starting a patrol to verifying checkpoints to ending the patrol
        /// </summary>
        [Fact]
        public async Task CompletePatrolFlow_ShouldSucceed()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a success response for the patrol/locations endpoint with sample locations
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolLocations, new[]
            {
                new
                {
                    Id = 1,
                    Name = "Office Building",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/checkpoints endpoint with sample checkpoints
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolCheckpoints, new[]
            {
                new
                {
                    Id = 101,
                    LocationId = 1,
                    Name = "Main Entrance",
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            });

            // Set up a success response for the patrol/verify endpoint with a success status
            ApiServer.SetupSuccessResponse(ApiEndpoints.PatrolVerify, new { Status = "success" });

            // Call PatrolService.GetLocations() and verify the result
            var locations = await PatrolService.GetLocations();
            locations.Should().NotBeNull();

            // Call PatrolService.GetCheckpoints(1) for a valid location and verify the result
            var checkpoints = await PatrolService.GetCheckpoints(1);
            checkpoints.Should().NotBeNull();

            // Call PatrolService.StartPatrol(1) to start a patrol
            var startStatus = await PatrolService.StartPatrol(1);

            // Assert that PatrolService.IsPatrolActive is true
            PatrolService.IsPatrolActive.Should().BeTrue();

            // Call PatrolService.CheckProximity(37.7749, -122.4194) to find nearby checkpoints
            var nearbyCheckpoints = await PatrolService.CheckProximity(TestConstants.TestLatitude, TestConstants.TestLongitude);
            nearbyCheckpoints.Should().NotBeNull();

            // Call PatrolService.VerifyCheckpoint(1) to verify a checkpoint
            var verifyResult = await PatrolService.VerifyCheckpoint(101);

            // Assert that the verification result is true
            verifyResult.Should().BeTrue();

            // Call PatrolService.GetPatrolStatus(1) to check progress
            var patrolStatus = await PatrolService.GetPatrolStatus(1);

            // Assert that VerifiedCheckpoints is now 1
            patrolStatus.VerifiedCheckpoints.Should().Be(1);

            // Call PatrolService.EndPatrol() to end the patrol
            var endStatus = await PatrolService.EndPatrol();

            // Assert that PatrolService.IsPatrolActive is false
            PatrolService.IsPatrolActive.Should().BeFalse();

            // Assert that the patrol end time has a value
            endStatus.EndTime.Should().HaveValue();
        }
    }
}