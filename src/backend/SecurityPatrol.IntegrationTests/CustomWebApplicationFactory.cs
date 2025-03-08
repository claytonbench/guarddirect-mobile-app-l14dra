using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecurityPatrol.API;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.IntegrationTests.Helpers;
using System;
using System.Linq;

namespace SecurityPatrol.IntegrationTests
{
    /// <summary>
    /// Custom WebApplicationFactory that configures a test server with in-memory database and test authentication for integration testing.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Gets the user ID used for test authentication.
        /// </summary>
        public string TestUserId { get; private set; }

        /// <summary>
        /// Gets the phone number used for test authentication.
        /// </summary>
        public string TestPhoneNumber { get; private set; }

        /// <summary>
        /// Gets the name of the in-memory database used for testing.
        /// </summary>
        public string DatabaseName { get; private set; }

        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the CustomWebApplicationFactory class.
        /// </summary>
        public CustomWebApplicationFactory()
        {
            TestUserId = Utilities.GetTestUserId();
            TestPhoneNumber = Utilities.GetTestPhoneNumber();
            DatabaseName = $"InMemoryTestDb_{Guid.NewGuid()}";
            _serviceProvider = null;
        }

        /// <summary>
        /// Configures the web host with test services, including in-memory database and test authentication.
        /// </summary>
        /// <param name="builder">The web host builder to configure.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            // Configure the environment as test
            builder.UseEnvironment("Test");

            // Configure services
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SecurityPatrolDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext with in-memory database
                services.AddDbContext<SecurityPatrolDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                // Add test authentication
                services.AddTestAuthentication(TestUserId, TestPhoneNumber);

                // Build service provider for later use
                _serviceProvider = services.BuildServiceProvider();
            });
        }

        /// <summary>
        /// Creates a new instance of the database context for test operations.
        /// </summary>
        /// <returns>A database context for test operations.</returns>
        public SecurityPatrolDbContext CreateDbContext()
        {
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<SecurityPatrolDbContext>();
        }

        /// <summary>
        /// Resets the database to a clean state with fresh test data.
        /// </summary>
        public void ResetDatabase()
        {
            using var context = CreateDbContext();
            Utilities.ReinitializeDbForTests(context);
        }

        /// <summary>
        /// Disposes resources used by the factory.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}