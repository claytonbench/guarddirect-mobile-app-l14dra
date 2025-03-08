using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the verification code entry screen in the authentication flow.
    /// Handles verification code validation, authentication completion, and navigation after successful authentication.
    /// </summary>
    public partial class AuthenticationViewModel : BaseViewModel
    {
        // Properties
        [ObservableProperty]
        private string phoneNumber;

        [ObservableProperty]
        private string verificationCode;

        [ObservableProperty]
        private bool isVerificationCodeValid;

        /// <summary>
        /// Gets the authentication service used for verification operations.
        /// </summary>
        public IAuthenticationService AuthenticationService { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Service used for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service used to access the authentication state.</param>
        /// <param name="authenticationService">Service used for authentication operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if authenticationService is null.</exception>
        public AuthenticationViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IAuthenticationService authenticationService)
            : base(navigationService, authenticationStateProvider)
        {
            AuthenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            VerificationCode = string.Empty;
            IsVerificationCodeValid = false;
            Title = "Verification";
        }

        /// <summary>
        /// Initializes the ViewModel when navigated to, retrieving the phone number from navigation parameters.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Check if user is already authenticated
            if (await IsUserAuthenticated())
            {
                await NavigationService.NavigateToAsync(NavigationConstants.MainPage);
                return;
            }

            // Get the phone number from the navigation parameter
            var phoneNumber = NavigationService.GetRouteParameter(NavigationConstants.ParamPhoneNumber) as string;
            
            if (string.IsNullOrEmpty(phoneNumber))
            {
                // Invalid state, navigate back
                await NavigationService.NavigateBackAsync();
                return;
            }

            PhoneNumber = phoneNumber;
        }

        /// <summary>
        /// Validates the current verification code.
        /// </summary>
        /// <returns>True if the verification code is valid, otherwise false.</returns>
        [RelayCommand]
        private bool ValidateVerificationCode()
        {
            var (isValid, errorMessage) = ValidationHelper.ValidateVerificationCode(VerificationCode);
            
            if (!isValid)
            {
                SetError(errorMessage);
                IsVerificationCodeValid = false;
                return false;
            }

            ClearError();
            IsVerificationCodeValid = true;
            return true;
        }

        /// <summary>
        /// Verifies the entered code and completes the authentication process.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AsyncRelayCommand]
        private async Task VerifyCodeAsync()
        {
            if (!ValidateVerificationCode())
            {
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                try
                {
                    bool success = await AuthenticationService.VerifyCode(VerificationCode);
                    
                    if (success)
                    {
                        await NavigationService.NavigateToAsync(NavigationConstants.MainPage);
                    }
                    else
                    {
                        SetError(ErrorMessages.AuthenticationFailed);
                    }
                }
                catch (Exception)
                {
                    SetError(ErrorMessages.NetworkError);
                }
            });
        }

        /// <summary>
        /// Handles changes to the verification code property.
        /// </summary>
        partial void OnVerificationCodeChanged(string value)
        {
            ClearError();
            IsVerificationCodeValid = false;
        }

        /// <summary>
        /// Requests a new verification code to be sent to the phone number.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AsyncRelayCommand]
        private async Task ResendVerificationCodeAsync()
        {
            if (string.IsNullOrEmpty(PhoneNumber))
            {
                SetError("Phone number is missing. Please go back and enter your phone number.");
                return;
            }

            await ExecuteWithBusyIndicator(async () =>
            {
                try
                {
                    bool success = await AuthenticationService.RequestVerificationCode(PhoneNumber);
                    
                    if (success)
                    {
                        // Show success message
                        ClearError();
                        // We could use a toast notification or status message here
                        // to indicate code was sent successfully
                    }
                    else
                    {
                        SetError(ErrorMessages.AuthenticationFailed);
                    }
                }
                catch (Exception)
                {
                    SetError(ErrorMessages.NetworkError);
                }
            });
        }
    }
}