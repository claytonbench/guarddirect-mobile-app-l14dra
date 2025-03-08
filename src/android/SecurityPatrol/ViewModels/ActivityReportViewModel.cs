using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the Activity Report page that handles the creation and submission of activity reports.
    /// </summary>
    public class ActivityReportViewModel : BaseViewModel
    {
        private string _reportText;
        /// <summary>
        /// Gets or sets the text content of the activity report.
        /// </summary>
        public string ReportText
        {
            get => _reportText;
            set
            {
                if (SetProperty(ref _reportText, value))
                {
                    OnReportTextChanged();
                }
            }
        }

        private int _remainingCharacters;
        /// <summary>
        /// Gets or sets the number of characters remaining for the report text.
        /// </summary>
        public int RemainingCharacters
        {
            get => _remainingCharacters;
            set => SetProperty(ref _remainingCharacters, value);
        }

        private bool _canSubmit;
        /// <summary>
        /// Gets or sets a value indicating whether the report can be submitted.
        /// </summary>
        public bool CanSubmit
        {
            get => _canSubmit;
            set => SetProperty(ref _canSubmit, value);
        }

        /// <summary>
        /// Gets the report service used for report operations.
        /// </summary>
        public IReportService ReportService { get; }

        /// <summary>
        /// Gets the location service used for location operations.
        /// </summary>
        public ILocationService LocationService { get; }

        /// <summary>
        /// Gets the command to submit the activity report.
        /// </summary>
        public IRelayCommand SubmitReportCommand { get; }

        /// <summary>
        /// Gets the command to cancel report creation and navigate back.
        /// </summary>
        public IRelayCommand CancelCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityReportViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Service for navigation between pages.</param>
        /// <param name="authenticationStateProvider">Service for authentication state.</param>
        /// <param name="reportService">Service for report operations.</param>
        /// <param name="locationService">Service for location operations.</param>
        public ActivityReportViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IReportService reportService,
            ILocationService locationService)
            : base(navigationService, authenticationStateProvider)
        {
            ReportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            LocationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            
            Title = "Activity Report";
            
            ReportText = string.Empty;
            RemainingCharacters = AppConstants.ReportMaxLength;
            CanSubmit = false;
            
            SubmitReportCommand = new AsyncRelayCommand(SubmitReportAsync, () => CanSubmit);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
        }

        /// <summary>
        /// Initializes the ViewModel when navigated to.
        /// </summary>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            ResetForm();
        }

        /// <summary>
        /// Resets the form to its initial state.
        /// </summary>
        private void ResetForm()
        {
            ReportText = string.Empty;
            RemainingCharacters = AppConstants.ReportMaxLength;
            CanSubmit = false;
            ClearError();
        }

        /// <summary>
        /// Handles changes to the report text, updating character count and validation.
        /// </summary>
        private void OnReportTextChanged()
        {
            RemainingCharacters = AppConstants.ReportMaxLength - (ReportText?.Length ?? 0);
            
            var (isValid, errorMessage) = ValidationHelper.ValidateReportText(ReportText);
            CanSubmit = isValid;
            
            if (!isValid)
                SetError(errorMessage);
            else
                ClearError();
        }

        /// <summary>
        /// Submits the activity report with the current text and location.
        /// </summary>
        private async Task SubmitReportAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                // Validate report text
                var (isValid, errorMessage) = ValidationHelper.ValidateReportText(ReportText);
                if (!isValid)
                {
                    SetError(errorMessage);
                    return;
                }

                try
                {
                    // Get current location
                    double latitude = 0;
                    double longitude = 0;
                    
                    try
                    {
                        var location = await LocationService.GetCurrentLocation();
                        latitude = location.Latitude;
                        longitude = location.Longitude;
                    }
                    catch (Exception)
                    {
                        // If location services fail, use default coordinates (0,0)
                        // This allows reports to be created even if location is unavailable
                        SetError(ErrorMessages.LocationServiceDisabled);
                    }

                    // Create report
                    var report = await ReportService.CreateReportAsync(ReportText, latitude, longitude);
                    
                    // Try to sync the report immediately if possible
                    if (report != null)
                    {
                        await ReportService.SyncReportAsync(report.Id);
                    }

                    // Show success message
                    await DialogHelper.DisplaySuccessAsync("Report submitted successfully.");

                    // Navigate back to report list
                    await NavigationService.NavigateToAsync(NavigationConstants.ReportListPage);
                }
                catch (Exception)
                {
                    SetError(ErrorMessages.ReportSubmissionFailed);
                }
            });
        }

        /// <summary>
        /// Cancels report creation and navigates back.
        /// </summary>
        private async Task CancelAsync()
        {
            await NavigationService.NavigateBackAsync();
        }
    }
}