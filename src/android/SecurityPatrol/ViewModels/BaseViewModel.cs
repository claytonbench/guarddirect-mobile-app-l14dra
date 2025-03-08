using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityPatrol.Services;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels in the Security Patrol application, providing common functionality
    /// such as navigation, authentication state access, busy indicator management, error handling,
    /// and property change notification.
    /// </summary>
    public class BaseViewModel : ObservableObject, IDisposable
    {
        /// <summary>
        /// Gets the navigation service used to navigate between pages.
        /// </summary>
        protected INavigationService NavigationService { get; }

        /// <summary>
        /// Gets the authentication state provider used to access the current authentication state.
        /// </summary>
        protected IAuthenticationStateProvider AuthenticationStateProvider { get; }

        private string _title;
        /// <summary>
        /// Gets or sets the title of the page associated with this ViewModel.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _isBusy;
        /// <summary>
        /// Gets or sets a value indicating whether the ViewModel is currently busy.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, onChanged: () => OnPropertyChanged(nameof(IsNotBusy)));
        }

        /// <summary>
        /// Gets a value indicating whether the ViewModel is not busy (inverse of IsBusy).
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        private string _errorMessage;
        /// <summary>
        /// Gets or sets the current error message, if any.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value, onChanged: () => OnPropertyChanged(nameof(HasError)));
        }

        /// <summary>
        /// Gets a value indicating whether there is an error message.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Service used for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service used to access the authentication state.</param>
        /// <exception cref="ArgumentNullException">Thrown if either navigationService or authenticationStateProvider is null.</exception>
        public BaseViewModel(INavigationService navigationService, IAuthenticationStateProvider authenticationStateProvider)
        {
            NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            AuthenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
            
            Title = string.Empty;
            _isBusy = false;
            _errorMessage = null;
        }

        /// <summary>
        /// Initializes the ViewModel. This method is called when the associated page is navigated to.
        /// Override this method to perform initialization logic in derived ViewModels.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the associated page is navigated to.
        /// </summary>
        /// <param name="parameters">Optional navigation parameters.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual async Task OnNavigatedTo(Dictionary<string, object> parameters = null)
        {
            await InitializeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when navigating away from the associated page.
        /// Override this method to perform cleanup in derived ViewModels.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual Task OnNavigatedFrom()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the busy state of the ViewModel.
        /// </summary>
        /// <param name="isBusy">Whether the ViewModel is busy.</param>
        protected void SetBusy(bool isBusy)
        {
            IsBusy = isBusy;
        }

        /// <summary>
        /// Executes an asynchronous action while displaying a busy indicator.
        /// Automatically handles errors and updates the busy state.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if action is null.</exception>
        protected async Task ExecuteWithBusyIndicator(Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            SetBusy(true);
            ClearError();

            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Executes an asynchronous function that returns a value while displaying a busy indicator.
        /// Automatically handles errors and updates the busy state.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="action">The asynchronous function to execute.</param>
        /// <returns>A <see cref="Task{T}"/> representing the asynchronous operation with its result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if action is null.</exception>
        protected async Task<T> ExecuteWithBusyIndicator<T>(Func<Task<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            SetBusy(true);
            ClearError();
            
            T result = default;
            
            try
            {
                result = await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
            
            return result;
        }

        /// <summary>
        /// Sets an error message and updates the HasError property.
        /// </summary>
        /// <param name="message">The error message to set.</param>
        protected void SetError(string message)
        {
            ErrorMessage = message;
        }

        /// <summary>
        /// Clears the current error message and resets the HasError property.
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = null;
        }

        /// <summary>
        /// Checks if the current user is authenticated.
        /// </summary>
        /// <returns>True if the user is authenticated; otherwise, false.</returns>
        protected async Task<bool> IsUserAuthenticated()
        {
            return await AuthenticationStateProvider.IsAuthenticated().ConfigureAwait(false);
        }

        /// <summary>
        /// Releases resources used by the ViewModel.
        /// </summary>
        public virtual void Dispose()
        {
            // Base implementation does nothing.
            // Derived classes should override this method to release resources.
        }
    }
}