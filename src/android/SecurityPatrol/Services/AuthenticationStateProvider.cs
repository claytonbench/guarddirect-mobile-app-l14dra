using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging, version 8.0.0
using SecurityPatrol.Constants;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of IAuthenticationStateProvider that manages and provides access 
    /// to the user's authentication state throughout the application.
    /// </summary>
    public class AuthenticationStateProvider : IAuthenticationStateProvider
    {
        private AuthState _currentState;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<AuthenticationStateProvider> _logger;

        /// <summary>
        /// Event that is raised when the authentication state changes.
        /// </summary>
        public event EventHandler StateChanged;

        /// <summary>
        /// Initializes a new instance of the AuthenticationStateProvider class with required dependencies.
        /// </summary>
        /// <param name="settingsService">The settings service for persisting authentication state.</param>
        /// <param name="logger">The logger for tracking authentication operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when settingsService or logger is null.</exception>
        public AuthenticationStateProvider(ISettingsService settingsService, ILogger<AuthenticationStateProvider> logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Load the persisted state or create a new unauthenticated state if none exists
            _currentState = LoadPersistedState();
            _logger.LogInformation("AuthenticationStateProvider initialized with state: IsAuthenticated={IsAuthenticated}", 
                _currentState.IsAuthenticated);
        }

        /// <summary>
        /// Retrieves the current authentication state of the user.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing the current authentication state.</returns>
        public Task<AuthState> GetCurrentState()
        {
            _logger.LogInformation("Getting current authentication state");
            return Task.FromResult(_currentState);
        }

        /// <summary>
        /// Updates the current authentication state and notifies subscribers.
        /// </summary>
        /// <param name="state">The new authentication state.</param>
        /// <exception cref="ArgumentNullException">Thrown when state is null.</exception>
        public void UpdateState(AuthState state)
        {
            _logger.LogInformation("Updating authentication state: IsAuthenticated={IsAuthenticated}, PhoneNumber={PhoneNumber}",
                state?.IsAuthenticated, state?.PhoneNumber);
                
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            
            _currentState = state;
            
            // Persist the state to settings
            _settingsService.SetValue(AppConstants.AuthStateKey, _currentState);
            
            // Notify subscribers of the state change
            NotifyStateChanged();
            
            _logger.LogInformation("Authentication state updated successfully");
        }

        /// <summary>
        /// Checks if the user is currently authenticated.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation, containing a boolean value 
        /// indicating whether the user is authenticated (true) or not (false).</returns>
        public Task<bool> IsAuthenticated()
        {
            _logger.LogInformation("Checking if user is authenticated");
            return Task.FromResult(_currentState.IsAuthenticated);
        }

        /// <summary>
        /// Notifies subscribers that the authentication state has changed.
        /// </summary>
        public void NotifyStateChanged()
        {
            _logger.LogInformation("Notifying subscribers of authentication state change");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Loads the authentication state from persistent storage.
        /// </summary>
        /// <returns>The loaded authentication state or a new unauthenticated state if none exists.</returns>
        private AuthState LoadPersistedState()
        {
            _logger.LogInformation("Loading persisted authentication state");
            var state = _settingsService.GetValue<AuthState>(AppConstants.AuthStateKey, null);
            
            if (state != null)
            {
                _logger.LogInformation("Loaded persisted authentication state: IsAuthenticated={IsAuthenticated}, PhoneNumber={PhoneNumber}", 
                    state.IsAuthenticated, state.PhoneNumber);
                return state;
            }
            
            _logger.LogInformation("No persisted authentication state found, creating unauthenticated state");
            return AuthState.CreateUnauthenticated();
        }
    }
}