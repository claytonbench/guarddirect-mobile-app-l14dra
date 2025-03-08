using System; // version 8.0.0

namespace SecurityPatrol.Constants
{
    /// <summary>
    /// Static class containing constant string values for navigation routes and parameters.
    /// Centralizes all navigation-related constants to ensure consistency across the application.
    /// </summary>
    public static class NavigationConstants
    {
        #region Page Routes

        /// <summary>
        /// Route to the phone number entry page for authentication.
        /// </summary>
        public const string PhoneEntryPage = "PhoneEntryPage";

        /// <summary>
        /// Route to the verification code entry page.
        /// </summary>
        public const string VerificationPage = "VerificationPage";

        /// <summary>
        /// Route to the main dashboard page.
        /// </summary>
        public const string MainPage = "MainPage";

        /// <summary>
        /// Route to the time tracking page for clock in/out functionality.
        /// </summary>
        public const string TimeTrackingPage = "TimeTrackingPage";

        /// <summary>
        /// Route to the time history page showing clock in/out records.
        /// </summary>
        public const string TimeHistoryPage = "TimeHistoryPage";

        /// <summary>
        /// Route to the patrol management page.
        /// </summary>
        public const string PatrolPage = "PatrolPage";

        /// <summary>
        /// Route to the location selection page for patrol locations.
        /// </summary>
        public const string LocationSelectionPage = "LocationSelectionPage";

        /// <summary>
        /// Route to the checkpoint list page showing checkpoints for a location.
        /// </summary>
        public const string CheckpointListPage = "CheckpointListPage";

        /// <summary>
        /// Route to the photo capture page.
        /// </summary>
        public const string PhotoCapturePage = "PhotoCapturePage";

        /// <summary>
        /// Route to the photo gallery page showing all captured photos.
        /// </summary>
        public const string PhotoGalleryPage = "PhotoGalleryPage";

        /// <summary>
        /// Route to the photo detail page for viewing a specific photo.
        /// </summary>
        public const string PhotoDetailPage = "PhotoDetailPage";

        /// <summary>
        /// Route to the activity report creation page.
        /// </summary>
        public const string ActivityReportPage = "ActivityReportPage";

        /// <summary>
        /// Route to the report list page showing all activity reports.
        /// </summary>
        public const string ReportListPage = "ReportListPage";

        /// <summary>
        /// Route to the report details page for viewing a specific report.
        /// </summary>
        public const string ReportDetailsPage = "ReportDetailsPage";

        /// <summary>
        /// Route to the settings page.
        /// </summary>
        public const string SettingsPage = "SettingsPage";

        #endregion

        #region Navigation Parameters

        /// <summary>
        /// Parameter key for passing phone number between pages.
        /// </summary>
        public const string ParamPhoneNumber = "phoneNumber";

        /// <summary>
        /// Parameter key indicating if verification code has been requested.
        /// </summary>
        public const string ParamVerificationRequested = "verificationRequested";

        /// <summary>
        /// Parameter key for passing photo ID between pages.
        /// </summary>
        public const string ParamPhotoId = "photoId";

        /// <summary>
        /// Parameter key for passing report ID between pages.
        /// </summary>
        public const string ParamReportId = "reportId";

        /// <summary>
        /// Parameter key for passing location ID between pages.
        /// </summary>
        public const string ParamLocationId = "locationId";

        /// <summary>
        /// Parameter key for passing checkpoint ID between pages.
        /// </summary>
        public const string ParamCheckpointId = "checkpointId";

        #endregion

        // Private constructor to prevent instantiation of static class
        private NavigationConstants()
        {
            // This constructor is intentionally empty.
            // Prevents instantiation of this static class.
        }
    }
}