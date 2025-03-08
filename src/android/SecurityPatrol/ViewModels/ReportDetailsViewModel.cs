using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.ViewModels
{
    /// <summary>
    /// ViewModel for the Report Details page that handles viewing, editing, and deleting activity reports in the Security Patrol application.
    /// </summary>
    public class ReportDetailsViewModel : BaseViewModel
    {
        #region Properties

        private int _reportId;
        /// <summary>
        /// Gets or sets the ID of the report being viewed or edited.
        /// </summary>
        public int ReportId
        {
            get => _reportId;
            set => SetProperty(ref _reportId, value);
        }

        private ReportModel _report;
        /// <summary>
        /// Gets or sets the report model being viewed or edited.
        /// </summary>
        public ReportModel Report
        {
            get => _report;
            set => SetProperty(ref _report, value);
        }

        private ReportModel _originalReport;
        /// <summary>
        /// Gets or sets the original report model used to track changes during editing.
        /// </summary>
        public ReportModel OriginalReport
        {
            get => _originalReport;
            private set => _originalReport = value;
        }
        
        private string _reportText;
        /// <summary>
        /// Gets or sets the text content of the report.
        /// </summary>
        public string ReportText
        {
            get => _reportText;
            set
            {
                SetProperty(ref _reportText, value);
                OnReportTextChanged();
            }
        }

        private DateTime _timestamp;
        /// <summary>
        /// Gets or sets the timestamp when the report was created.
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        private string _locationText;
        /// <summary>
        /// Gets or sets the formatted location text where the report was created.
        /// </summary>
        public string LocationText
        {
            get => _locationText;
            set => SetProperty(ref _locationText, value);
        }

        private bool _isSynced;
        /// <summary>
        /// Gets or sets whether the report has been synchronized with the backend.
        /// </summary>
        public bool IsSynced
        {
            get => _isSynced;
            set => SetProperty(ref _isSynced, value);
        }

        private int _remainingCharacters;
        /// <summary>
        /// Gets or sets the number of remaining characters allowed in the report text.
        /// </summary>
        public int RemainingCharacters
        {
            get => _remainingCharacters;
            set => SetProperty(ref _remainingCharacters, value);
        }

        private bool _isEditing;
        /// <summary>
        /// Gets or sets whether the report is currently being edited.
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private bool _hasChanges;
        /// <summary>
        /// Gets or sets whether the report has unsaved changes.
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        private bool _canSave;
        /// <summary>
        /// Gets or sets whether the report can be saved (valid and has changes).
        /// </summary>
        public bool CanSave
        {
            get => _canSave;
            set => SetProperty(ref _canSave, value);
        }

        #endregion

        #region Services and Commands

        /// <summary>
        /// The service responsible for report operations.
        /// </summary>
        private readonly IReportService ReportService;

        /// <summary>
        /// Command to enter edit mode.
        /// </summary>
        public IRelayCommand EditCommand { get; }

        /// <summary>
        /// Command to save report changes.
        /// </summary>
        public IRelayCommand SaveCommand { get; }

        /// <summary>
        /// Command to cancel edit mode and discard changes.
        /// </summary>
        public IRelayCommand CancelEditCommand { get; }

        /// <summary>
        /// Command to delete the report.
        /// </summary>
        public IRelayCommand DeleteCommand { get; }

        /// <summary>
        /// Command to synchronize the report with the backend.
        /// </summary>
        public IRelayCommand SyncCommand { get; }

        /// <summary>
        /// Command to navigate back to the previous page.
        /// </summary>
        public IRelayCommand BackCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportDetailsViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Service for navigating between pages.</param>
        /// <param name="authenticationStateProvider">Service for accessing authentication state.</param>
        /// <param name="reportService">Service for report operations.</param>
        public ReportDetailsViewModel(
            INavigationService navigationService,
            IAuthenticationStateProvider authenticationStateProvider,
            IReportService reportService)
            : base(navigationService, authenticationStateProvider)
        {
            ReportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            
            Title = "Report Details";
            
            // Initialize commands
            EditCommand = new RelayCommand(StartEditing);
            SaveCommand = new AsyncRelayCommand(SaveReportAsync, () => CanSave);
            CancelEditCommand = new RelayCommand(CancelEditing);
            DeleteCommand = new AsyncRelayCommand(DeleteReportAsync);
            SyncCommand = new AsyncRelayCommand(SyncReportAsync);
            BackCommand = new AsyncRelayCommand(GoBackAsync);
            
            // Initialize default values
            ReportId = 0;
            Report = null;
            OriginalReport = null;
            ReportText = string.Empty;
            Timestamp = DateTime.Now;
            LocationText = string.Empty;
            IsSynced = false;
            RemainingCharacters = AppConstants.ReportMaxLength;
            IsEditing = false;
            HasChanges = false;
            CanSave = false;
        }

        #region Lifecycle Methods

        /// <summary>
        /// Initializes the ViewModel when navigating to the page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            if (ReportId > 0)
            {
                await LoadReportAsync();
            }
        }

        /// <summary>
        /// Called when navigating to the page with parameters.
        /// </summary>
        /// <param name="parameters">Navigation parameters.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnNavigatedTo(Dictionary<string, object> parameters = null)
        {
            await base.OnNavigatedTo(parameters);
            
            if (parameters != null && parameters.TryGetValue(NavigationConstants.ParamReportId, out var reportIdObj))
            {
                if (int.TryParse(reportIdObj.ToString(), out var reportId))
                {
                    ReportId = reportId;
                    await InitializeAsync();
                }
            }
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        public void OnAppearing()
        {
            // Reset editing state when page appears
            IsEditing = false;
            HasChanges = false;
            CanSave = false;
            
            // Load report if not loaded yet and we have a valid ID
            if (Report == null && ReportId > 0)
            {
                LoadReportAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        public void OnDisappearing()
        {
            // If editing with changes, this would be a good place to prompt to save changes
            // In a real implementation, we would show a dialog here
            if (IsEditing && HasChanges)
            {
                IsEditing = false;
                HasChanges = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the report data from the repository.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LoadReportAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                try
                {
                    var report = await ReportService.GetReportAsync(ReportId);
                    if (report == null)
                    {
                        SetError(ErrorMessages.ReportNotFound);
                        return;
                    }
                    
                    Report = report;
                    OriginalReport = report.Clone();
                    
                    ReportText = Report.Text;
                    Timestamp = Report.Timestamp;
                    LocationText = $"Latitude: {Report.Latitude:F6}, Longitude: {Report.Longitude:F6}";
                    IsSynced = Report.IsSynced;
                    RemainingCharacters = AppConstants.ReportMaxLength - ReportText.Length;
                }
                catch (Exception ex)
                {
                    SetError(ex.Message);
                }
            });
        }

        /// <summary>
        /// Handles changes to the report text, updating character count and validation.
        /// </summary>
        private void OnReportTextChanged()
        {
            // Update remaining characters
            RemainingCharacters = AppConstants.ReportMaxLength - (ReportText?.Length ?? 0);
            
            // Validate report text
            var (isValid, errorMessage) = ValidationHelper.ValidateReportText(ReportText);
            
            if (!isValid)
            {
                SetError(errorMessage);
                CanSave = false;
            }
            else
            {
                ClearError();
                CheckForChanges();
            }
        }

        /// <summary>
        /// Puts the ViewModel into editing mode.
        /// </summary>
        private void StartEditing()
        {
            IsEditing = true;
            ClearError();
        }

        /// <summary>
        /// Cancels editing mode and reverts changes.
        /// </summary>
        private void CancelEditing()
        {
            IsEditing = false;
            if (OriginalReport != null)
            {
                ReportText = OriginalReport.Text;
            }
            HasChanges = false;
            CanSave = false;
            ClearError();
        }

        /// <summary>
        /// Saves changes to the report.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SaveReportAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                var (isValid, errorMessage) = ValidationHelper.ValidateReportText(ReportText);
                if (!isValid)
                {
                    SetError(errorMessage);
                    return;
                }
                
                try
                {
                    // Update report text
                    Report.Text = ReportText;
                    
                    // Save changes
                    bool success = await ReportService.UpdateReportAsync(Report);
                    if (success)
                    {
                        OriginalReport = Report.Clone();
                        IsEditing = false;
                        HasChanges = false;
                        
                        await DialogHelper.DisplaySuccessAsync("Report updated successfully.");
                        
                        // Try to sync report if network is available
                        try
                        {
                            await ReportService.SyncReportAsync(Report.Id);
                            await LoadReportAsync(); // Reload to get updated sync status
                        }
                        catch
                        {
                            // Sync failed but update was successful, so we don't show an error
                        }
                    }
                    else
                    {
                        SetError(ErrorMessages.ReportUpdateFailed);
                    }
                }
                catch (Exception)
                {
                    SetError(ErrorMessages.ReportUpdateFailed);
                }
            });
        }

        /// <summary>
        /// Deletes the current report after confirmation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DeleteReportAsync()
        {
            // Confirm deletion
            bool confirm = await DialogHelper.DisplayConfirmationAsync(
                "Delete Report", 
                "Are you sure you want to delete this report?", 
                "Delete", 
                "Cancel");
            
            if (confirm)
            {
                await ExecuteWithBusyIndicator(async () =>
                {
                    try
                    {
                        bool success = await ReportService.DeleteReportAsync(ReportId);
                        if (success)
                        {
                            await DialogHelper.DisplaySuccessAsync("Report deleted successfully.");
                            
                            // Navigate back to report list
                            await NavigationService.NavigateToAsync(NavigationConstants.ReportListPage);
                        }
                        else
                        {
                            SetError(ErrorMessages.ReportDeleteFailed);
                        }
                    }
                    catch (Exception)
                    {
                        SetError(ErrorMessages.ReportDeleteFailed);
                    }
                });
            }
        }

        /// <summary>
        /// Synchronizes the report with the backend API.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SyncReportAsync()
        {
            await ExecuteWithBusyIndicator(async () =>
            {
                try
                {
                    bool success = await ReportService.SyncReportAsync(ReportId);
                    if (success)
                    {
                        await LoadReportAsync(); // Reload to get updated sync status
                        await DialogHelper.DisplaySuccessAsync("Report synchronized successfully.");
                    }
                    else
                    {
                        SetError("Synchronization failed. Please try again later.");
                    }
                }
                catch (Exception ex)
                {
                    SetError("Synchronization failed: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task GoBackAsync()
        {
            // If editing with changes, prompt to save
            if (IsEditing && HasChanges)
            {
                bool saveChanges = await DialogHelper.DisplayConfirmationAsync(
                    "Unsaved Changes", 
                    "Do you want to save your changes?", 
                    "Save", 
                    "Discard");
                
                if (saveChanges)
                {
                    await SaveReportAsync();
                }
            }
            
            // Navigate back
            await NavigationService.NavigateBackAsync();
        }

        /// <summary>
        /// Checks if the report text has been modified from the original.
        /// </summary>
        private void CheckForChanges()
        {
            if (OriginalReport == null)
            {
                HasChanges = !string.IsNullOrEmpty(ReportText);
            }
            else
            {
                HasChanges = ReportText != OriginalReport.Text;
            }
            
            // Can save if we have changes and the validation passes
            var (isValid, _) = ValidationHelper.ValidateReportText(ReportText);
            CanSave = HasChanges && isValid;
        }

        #endregion
    }
}