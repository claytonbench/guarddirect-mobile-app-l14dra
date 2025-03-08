using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SQLite;
using SecurityPatrol.Services;

namespace SecurityPatrol.Database.Repositories
{
    /// <summary>
    /// Abstract base class that provides common functionality for repository implementations
    /// in the Security Patrol application. It encapsulates database access operations,
    /// error handling, and transaction management to promote code reuse and consistent 
    /// implementation across different entity repositories.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that represents the database table</typeparam>
    /// <typeparam name="TModel">The model type that represents the domain object</typeparam>
    public abstract class BaseRepository<TEntity, TModel>
        where TEntity : new()
    {
        protected readonly IDatabaseService _databaseService;
        protected readonly ILogger _logger;
        protected readonly string TableName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository{TEntity, TModel}"/> class.
        /// </summary>
        /// <param name="databaseService">The database service for data access operations</param>
        /// <param name="logger">The logger for recording repository activities</param>
        protected BaseRepository(IDatabaseService databaseService, ILogger logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Set table name based on entity type name
            TableName = typeof(TEntity).Name;
            
            _logger.LogDebug($"Initialized {GetType().Name} for entity type {typeof(TEntity).Name}");
        }

        /// <summary>
        /// Gets an initialized SQLite connection to the database.
        /// </summary>
        /// <returns>A task that returns an initialized SQLite connection</returns>
        protected async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            try
            {
                return await _databaseService.GetConnectionAsync();
            }
            catch (Exception ex)
            {
                LogError("Failed to get database connection", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all entities from the database.
        /// </summary>
        /// <returns>A task that returns a list of all entities converted to models</returns>
        protected async Task<List<TModel>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation($"Retrieving all {typeof(TEntity).Name} entities");
                var connection = await GetConnectionAsync();
                var entities = await connection.Table<TEntity>().ToListAsync();
                
                _logger.LogDebug($"Retrieved {entities.Count} {typeof(TEntity).Name} entities");
                return entities.Select(ConvertToModel).ToList();
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred while retrieving all {typeof(TEntity).Name} entities", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred while retrieving all {typeof(TEntity).Name} entities", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to retrieve</param>
        /// <returns>A task that returns the entity with the specified ID converted to a model, or null if not found</returns>
        protected async Task<TModel> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Retrieving {typeof(TEntity).Name} with ID {id}");
                var connection = await GetConnectionAsync();
                var entity = await connection.FindAsync<TEntity>(id);
                
                if (entity == null)
                {
                    _logger.LogDebug($"{typeof(TEntity).Name} with ID {id} not found");
                    return default;
                }
                
                _logger.LogDebug($"Retrieved {typeof(TEntity).Name} with ID {id}");
                return ConvertToModel(entity);
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred while retrieving {typeof(TEntity).Name} with ID {id}", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred while retrieving {typeof(TEntity).Name} with ID {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves entities that match the specified expression.
        /// </summary>
        /// <param name="predicate">The expression to filter entities</param>
        /// <returns>A task that returns a list of entities that match the predicate converted to models</returns>
        protected async Task<List<TModel>> GetByExpressionAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                _logger.LogInformation($"Retrieving {typeof(TEntity).Name} entities by expression");
                var connection = await GetConnectionAsync();
                var entities = await connection.Table<TEntity>().Where(predicate).ToListAsync();
                
                _logger.LogDebug($"Retrieved {entities.Count} {typeof(TEntity).Name} entities by expression");
                return entities.Select(ConvertToModel).ToList();
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred while retrieving {typeof(TEntity).Name} entities by expression", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred while retrieving {typeof(TEntity).Name} entities by expression", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts a new entity into the database.
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>A task that returns the ID of the inserted entity</returns>
        protected async Task<int> InsertAsync(TEntity entity)
        {
            try
            {
                _logger.LogInformation($"Inserting new {typeof(TEntity).Name} entity");
                var connection = await GetConnectionAsync();
                var result = await connection.InsertAsync(entity);
                
                _logger.LogDebug($"Inserted {typeof(TEntity).Name} entity with result {result}");
                return result;
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred while inserting {typeof(TEntity).Name} entity", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred while inserting {typeof(TEntity).Name} entity", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing entity in the database.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>A task that returns the number of rows updated (should be 1 for success)</returns>
        protected async Task<int> UpdateAsync(TEntity entity)
        {
            try
            {
                _logger.LogInformation($"Updating {typeof(TEntity).Name} entity");
                var connection = await GetConnectionAsync();
                var result = await connection.UpdateAsync(entity);
                
                _logger.LogDebug($"Updated {typeof(TEntity).Name} entity with result {result}");
                return result;
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred while updating {typeof(TEntity).Name} entity", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred while updating {typeof(TEntity).Name} entity", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to delete</param>
        /// <returns>A task that returns the number of rows deleted (should be 1 for success)</returns>
        protected async Task<int> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting {typeof(TEntity).Name} with ID {id}");
                var connection = await GetConnectionAsync();
                var result = await connection.DeleteAsync<TEntity>(id);
                
                _logger.LogDebug($"Deleted {typeof(TEntity).Name} with ID {id}, result: {result}");
                return result;
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred while deleting {typeof(TEntity).Name} with ID {id}", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred while deleting {typeof(TEntity).Name} with ID {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the specified action within a transaction.
        /// </summary>
        /// <param name="action">The action to execute within the transaction</param>
        /// <returns>A task representing the asynchronous operation</returns>
        protected async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            try
            {
                _logger.LogInformation($"Starting transaction for {typeof(TEntity).Name} operations");
                await _databaseService.RunInTransactionAsync(action);
                _logger.LogInformation($"Transaction completed successfully for {typeof(TEntity).Name} operations");
            }
            catch (SQLiteException ex)
            {
                LogError($"Database error occurred during transaction for {typeof(TEntity).Name} operations", ex);
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error occurred during transaction for {typeof(TEntity).Name} operations", ex);
                throw;
            }
        }

        /// <summary>
        /// Converts an entity to a model. Must be implemented by derived classes.
        /// </summary>
        /// <param name="entity">The entity to convert</param>
        /// <returns>The entity converted to a model</returns>
        protected abstract TModel ConvertToModel(TEntity entity);

        /// <summary>
        /// Converts a model to an entity. Must be implemented by derived classes.
        /// </summary>
        /// <param name="model">The model to convert</param>
        /// <returns>The model converted to an entity</returns>
        protected abstract TEntity ConvertToEntity(TModel model);

        /// <summary>
        /// Logs an error with the specified message and exception.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="ex">The exception that occurred</param>
        protected void LogError(string message, Exception ex)
        {
            _logger.LogError(ex, $"{message} - Entity: {typeof(TEntity).Name}, Error: {ex.Message}");
        }
    }
}