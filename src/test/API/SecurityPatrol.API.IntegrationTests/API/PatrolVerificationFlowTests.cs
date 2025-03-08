using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.API.IntegrationTests.API
{
    /// <summary>
    /// Integration tests for the patrol verification flow in the Security Patrol API, testing the complete process
    /// of retrieving patrol locations, checkpoints, and verifying checkpoints during security patrols.
    /// </summary>
    public class PatrolVerificationFlowTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the PatrolVerificationFlowTests class with the test factory.
        /// </summary>
        /// <param name="factory">Factory that creates a test server with in-memory database</param>
        public PatrolVerificationFlowTests(CustomWebApplicationFactory factory) 
            : base(factory)
        {
        }

        /// <summary>
        /// Tests that the /patrol/locations endpoint successfully returns a list of patrol locations.
        /// </summary>
        [Fact]
        public async Task Should_Get_Patrol_Locations_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<List<LocationModel>>>("/api/v1/patrol/locations");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCountGreaterThan(0);
        }

        /// <summary>
        /// Tests that the /patrol/locations/{id} endpoint successfully returns a specific patrol location.
        /// </summary>
        [Fact]
        public async Task Should_Get_Patrol_Location_By_Id_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<LocationModel>>("/api/v1/patrol/locations/1");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(1);
            result.Data.Name.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that the /patrol/locations/{locationId}/checkpoints endpoint successfully returns
        /// checkpoints for a specific location.
        /// </summary>
        [Fact]
        public async Task Should_Get_Checkpoints_By_Location_Id_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<List<CheckpointModel>>>("/api/v1/patrol/locations/1/checkpoints");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCountGreaterThan(0);
            result.Data.Should().OnlyContain(c => c.LocationId == 1);
        }

        /// <summary>
        /// Tests that the /patrol/checkpoints/{id} endpoint successfully returns a specific checkpoint.
        /// </summary>
        [Fact]
        public async Task Should_Get_Checkpoint_By_Id_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<CheckpointModel>>("/api/v1/patrol/checkpoints/1");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(1);
            result.Data.Name.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that the /patrol/verify endpoint successfully processes a checkpoint verification request.
        /// </summary>
        [Fact]
        public async Task Should_Verify_Checkpoint_Successfully()
        {
            // Arrange
            AuthenticateClient();
            var request = new CheckpointVerificationRequest
            {
                CheckpointId = 1,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act
            var result = await PostAsync<CheckpointVerificationRequest, Result<CheckpointVerificationResponse>>(
                "/api/v1/patrol/verify", request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.CheckpointId.Should().Be(1);
            result.Data.IsVerified.Should().BeTrue();
            result.Data.Status.Should().Be("Verified");
        }

        /// <summary>
        /// Tests that the /patrol/locations/{locationId}/status endpoint successfully returns
        /// the patrol status for a specific location.
        /// </summary>
        [Fact]
        public async Task Should_Get_Patrol_Status_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<PatrolStatusModel>>("/api/v1/patrol/locations/1/status");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.LocationId.Should().Be(1);
            result.Data.TotalCheckpoints.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests that the /patrol/checkpoints/nearby endpoint successfully returns checkpoints
        /// near a specified location.
        /// </summary>
        [Fact]
        public async Task Should_Get_Nearby_Checkpoints_Successfully()
        {
            // Arrange
            AuthenticateClient();
            string queryParameters = $"latitude={TestConstants.TestLatitude}&longitude={TestConstants.TestLongitude}&radiusInMeters=500";

            // Act
            var result = await GetAsync<Result<List<CheckpointModel>>>($"/api/v1/patrol/checkpoints/nearby?{queryParameters}");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the /patrol/verifications endpoint successfully returns checkpoint verifications
        /// for the current user.
        /// </summary>
        [Fact]
        public async Task Should_Get_User_Verifications_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<List<CheckpointVerificationResponse>>>("/api/v1/patrol/verifications");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the /patrol/checkpoints/{checkpointId}/verified endpoint successfully checks
        /// if a checkpoint is verified by the current user.
        /// </summary>
        [Fact]
        public async Task Should_Check_If_Checkpoint_Is_Verified_Successfully()
        {
            // Arrange
            AuthenticateClient();

            // Act
            var result = await GetAsync<Result<bool>>("/api/v1/patrol/checkpoints/1/verified");

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeOfType<bool>();
        }

        /// <summary>
        /// Tests the complete patrol verification flow from retrieving locations and checkpoints
        /// to verifying checkpoints and checking patrol status.
        /// </summary>
        [Fact]
        public async Task Should_Complete_Full_Patrol_Verification_Flow()
        {
            // Arrange
            AuthenticateClient();

            // Step 1: Get patrol locations
            var locationsResult = await GetAsync<Result<List<LocationModel>>>("/api/v1/patrol/locations");
            locationsResult.Succeeded.Should().BeTrue();
            locationsResult.Data.Should().NotBeEmpty();
            
            // Select the first location
            var locationId = locationsResult.Data[0].Id;

            // Step 2: Get checkpoints for the selected location
            var checkpointsResult = await GetAsync<Result<List<CheckpointModel>>>($"/api/v1/patrol/locations/{locationId}/checkpoints");
            checkpointsResult.Succeeded.Should().BeTrue();
            checkpointsResult.Data.Should().NotBeEmpty();
            
            // Select the first checkpoint
            var checkpointId = checkpointsResult.Data[0].Id;

            // Step 3: Verify the checkpoint
            var verificationRequest = new CheckpointVerificationRequest
            {
                CheckpointId = checkpointId,
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            var verificationResult = await PostAsync<CheckpointVerificationRequest, Result<CheckpointVerificationResponse>>(
                "/api/v1/patrol/verify", verificationRequest);
            verificationResult.Succeeded.Should().BeTrue();
            verificationResult.Data.Should().NotBeNull();
            verificationResult.Data.IsVerified.Should().BeTrue();

            // Step 4: Check the patrol status to confirm the verification
            var statusResult = await GetAsync<Result<PatrolStatusModel>>($"/api/v1/patrol/locations/{locationId}/status");
            statusResult.Succeeded.Should().BeTrue();
            statusResult.Data.Should().NotBeNull();
            statusResult.Data.VerifiedCheckpoints.Should().BeGreaterThan(0);

            // Step 5: Check if the specific checkpoint is now verified
            var isVerifiedResult = await GetAsync<Result<bool>>($"/api/v1/patrol/checkpoints/{checkpointId}/verified");
            isVerifiedResult.Succeeded.Should().BeTrue();
            isVerifiedResult.Data.Should().BeTrue();
        }

        /// <summary>
        /// Tests that the /patrol/verify endpoint rejects a verification request with an invalid checkpoint ID.
        /// </summary>
        [Fact]
        public async Task Should_Reject_Invalid_Checkpoint_Verification()
        {
            // Arrange
            AuthenticateClient();
            var request = new CheckpointVerificationRequest
            {
                CheckpointId = -1, // Invalid ID
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude,
                    Longitude = TestConstants.TestLongitude
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                PostAsync<CheckpointVerificationRequest, Result<CheckpointVerificationResponse>>(
                    "/api/v1/patrol/verify", request)
            );
        }
    }
}