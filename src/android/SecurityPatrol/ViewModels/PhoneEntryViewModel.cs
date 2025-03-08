using System.Threading.Tasks; // System.Threading.Tasks 8.0.0
using System.Collections.Generic; // System.Collections.Generic 8.0.0
using CommunityToolkit.Mvvm.ComponentModel; // CommunityToolkit.Mvvm Latest
using CommunityToolkit.Mvvm.Input; // CommunityToolkit.Mvvm Latest
using SecurityPatrol.Constants;
using SecurityPatrol.Services;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the phone number entry screen in the authentication flow of the Security Patrol application.
    /// </summary>
    public partial class PhoneEntryViewModel : BaseViewModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the phone number entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _phoneNumber;

        /// <summary>
        /// Gets or sets a value indicating whether the current phone number is valid.
        /// </summary>
        [ObservableProperty]
        private bool _isPhoneNumberValid;

        /// <summary>
        /// Gets the authentication service used for verification code requests.
        /// </summary>
        public IAuthenticationService AuthenticationService { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneEntryViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The service used for navigation between pages.</param>
        /// <param name="authenticationStateProvider">The service used to access authentication state.</param>
        /// <param name="authenticationService">The service used for authentication operations.</param>
        public PhoneEntryViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IAuthenticationService authenticationService)
            : base(navigationService, authenticationStateProvider)
        {
            AuthenticationService = authenticationService ?? throw new System.ArgumentNullException(nameof(authenticationService));
            PhoneNumber = string.Empty;
            IsPhoneNumberValid = false;
            Title = "Phone Number";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the ViewModel when navigated to, checking if user is already authenticated.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            // If the user is already authenticated, navigate to the main page
            if (await IsUserAuthenticated())
            {
                await NavigationService.NavigateToAsync(NavigationConstants.MainPage);
            }
        }

        /// <summary>
        /// Validates the current phone number.
        /// </summary>
        /// <returns>True if the phone number is valid; otherwise, false.</returns>
        [RelayCommand]
        private bool ValidatePhoneNumber()
        {
            var (isValid, errorMessage) = ValidationHelper.ValidatePhoneNumber(PhoneNumber);
            
            if (!isValid)
            {
                SetError(errorMessage);
                IsPhoneNumberValid = false;
                return false;
            }
            
            ClearError();
            IsPhoneNumberValid = true;
            return true;
        }

        /// <summary>
        /// Requests a verification code to be sent to the entered phone number.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [AsyncRelayCommand]
        private async Task RequestVerificationCodeAsync()
        {
            if (!ValidatePhoneNumber())
            {
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                try
                {
                    bool success = await AuthenticationService.RequestVerificationCode(PhoneNumber);
                    
                    if (success)
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            { NavigationConstants.ParamPhoneNumber, PhoneNumber }
                        };
                        
                        await NavigationService.NavigateToAsync(NavigationConstants.VerificationPage, parameters);
                    }
                    else
                    {
                        SetError(ErrorMessages.AuthenticationFailed);
                    }
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    SetError(ErrorMessages.NetworkError);
                }
                catch (System.Exception ex)
                {
                    SetError(ex.Message);
                }
            });
        }

        /// <summary>
        /// Handles changes to the phone number property.
        /// </summary>
        partial void OnPhoneNumberChanged(string value)
        {
            ClearError();
            IsPhoneNumberValid = false;
        }

        #endregion
    }
}