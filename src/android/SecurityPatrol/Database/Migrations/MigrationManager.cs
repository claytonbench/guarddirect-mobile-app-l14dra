using Microsoft.Extensions.Logging;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.Database.Migrations
{
    /// <summary>
    /// Manages database migrations for the Security Patrol application, ensuring they are applied in the correct order based on version numbers.
    /// </summary>
    public class MigrationManager
    {
        private readonly ILogger<MigrationManager> _logger;
        private readonly List<IMigration> _migrations;

        /// <summary>
        /// Initializes a new instance of the MigrationManager class with optional logger.
        /// </summary>
        /// <param name="logger">Optional logger for migration operations</param>
        public MigrationManager(ILogger<MigrationManager> logger = null)
        {
            _logger = logger;
            _migrations = new List<IMigration>();
            RegisterMigrations();
            
            // Sort migrations by version number
            _migrations = _migrations.OrderBy(m => m.Version).ToList();
        }

        /// <summary>
        /// Applies all migrations with versions greater than the current database version.
        /// </summary>
        /// <param name="connection">The SQLite database connection</param>
        /// <param name="currentVersion">The current database version</param>
        /// <returns>A task that returns the new database version after applying migrations</returns>
        public async Task<double> ApplyMigrationsAsync(SQLiteAsyncConnection connection, double currentVersion)
        {
            _logger?.LogInformation($"Starting migration process from version {currentVersion}");

            var applicableMigrations = _migrations.Where(m => m.Version > currentVersion)
                                                 .OrderBy(m => m.Version)
                                                 .ToList();

            if (!applicableMigrations.Any())
            {
                _logger?.LogInformation("No applicable migrations found");
                return currentVersion;
            }

            foreach (var migration in applicableMigrations)
            {
                _logger?.LogInformation($"Applying migration {migration.GetType().Name} (version {migration.Version})");
                
                await migration.ApplyAsync(connection);
                currentVersion = migration.Version;
                
                _logger?.LogInformation($"Migration to version {currentVersion} completed successfully");
            }

            _logger?.LogInformation($"Migration process completed. New database version is {currentVersion}");
            return currentVersion;
        }

        /// <summary>
        /// Registers all available migrations by adding them to the migrations list.
        /// </summary>
        private void RegisterMigrations()
        {
            // Register all available migrations
            _migrations.Add(new Migration_1_0());
            _migrations.Add(new Migration_1_1());
            
            _logger?.LogInformation($"Registered {_migrations.Count} migrations");
        }
    }
}