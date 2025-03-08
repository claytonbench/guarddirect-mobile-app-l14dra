using System; // System 8.0.0

namespace SecurityPatrol.Constants
{
    /// <summary>
    /// Static class containing constant string values for all error messages used in the application.
    /// This centralizes error messages to ensure consistency in error reporting and simplify localization.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// Generic error message for unspecified errors.
        /// </summary>
        public static readonly string GenericError = "An error occurred. Please try again.";
        
        /// <summary>
        /// Error message for network connectivity issues.
        /// </summary>
        public static readonly string NetworkError = "Network connection unavailable. Please check your connection and try again.";
        
        /// <summary>
        /// Error message for server-side issues.
        /// </summary>
        public static readonly string ServerError = "Server error occurred. Please try again later.";
        
        /// <summary>
        /// Error message for operation timeouts.
        /// </summary>
        public static readonly string TimeoutError = "The operation timed out. Please try again.";
        
        /// <summary>
        /// Error message for failed authentication attempts.
        /// </summary>
        public static readonly string AuthenticationFailed = "Authentication failed. Please try again.";
        
        /// <summary>
        /// Error message for invalid credentials.
        /// </summary>
        public static readonly string InvalidCredentials = "Invalid credentials provided.";
        
        /// <summary>
        /// Error message for invalid phone number format.
        /// </summary>
        public static readonly string InvalidPhoneNumber = "Please enter a valid phone number with country code.";
        
        /// <summary>
        /// Error message for invalid verification code.
        /// </summary>
        public static readonly string InvalidVerificationCode = "Verification code must be 6 digits.";
        
        /// <summary>
        /// Error message for failed token refresh.
        /// </summary>
        public static readonly string TokenRefreshFailed = "Session refresh failed. Please log in again.";
        
        /// <summary>
        /// Error message for expired sessions.
        /// </summary>
        public static readonly string SessionExpired = "Your session has expired. Please log in again.";
        
        /// <summary>
        /// Error message for unauthorized access attempts.
        /// </summary>
        public static readonly string UnauthorizedAccess = "You do not have permission to perform this action.";
        
        /// <summary>
        /// Error message for denied location permissions.
        /// </summary>
        public static readonly string LocationPermissionDenied = "Location permission is required for this feature.";
        
        /// <summary>
        /// Error message for disabled location services.
        /// </summary>
        public static readonly string LocationServiceDisabled = "Location services are disabled. Please enable them in your device settings.";
        
        /// <summary>
        /// Error message for denied camera permissions.
        /// </summary>
        public static readonly string CameraPermissionDenied = "Camera permission is required for this feature.";
        
        /// <summary>
        /// Error message for denied storage permissions.
        /// </summary>
        public static readonly string StoragePermissionDenied = "Storage permission is required for this feature.";
        
        /// <summary>
        /// Error message for attempting to clock in when already clocked in.
        /// </summary>
        public static readonly string AlreadyClockedIn = "You are already clocked in.";
        
        /// <summary>
        /// Error message for attempting to clock out when already clocked out.
        /// </summary>
        public static readonly string AlreadyClockedOut = "You are already clocked out.";
        
        /// <summary>
        /// Error message for failed clock-in operations.
        /// </summary>
        public static readonly string ClockInFailed = "Failed to clock in. Please try again.";
        
        /// <summary>
        /// Error message for failed clock-out operations.
        /// </summary>
        public static readonly string ClockOutFailed = "Failed to clock out. Please try again.";
        
        /// <summary>
        /// Error message for empty report submissions.
        /// </summary>
        public static readonly string ReportEmpty = "Report text cannot be empty.";
        
        /// <summary>
        /// Error message for reports exceeding maximum length.
        /// </summary>
        public static readonly string ReportTooLong = "Report text exceeds maximum length.";
        
        /// <summary>
        /// Error message for failed report submissions.
        /// </summary>
        public static readonly string ReportSubmissionFailed = "Failed to submit report. Please try again.";
        
        /// <summary>
        /// Error message for failed photo capture.
        /// </summary>
        public static readonly string PhotoCaptureFailed = "Failed to capture photo. Please try again.";
        
        /// <summary>
        /// Error message for failed photo storage.
        /// </summary>
        public static readonly string PhotoStorageFailed = "Failed to store photo. Please check device storage.";
        
        /// <summary>
        /// Error message for failed photo uploads.
        /// </summary>
        public static readonly string PhotoUploadFailed = "Failed to upload photo. It will be uploaded when connection is available.";
        
        /// <summary>
        /// Error message for insufficient storage.
        /// </summary>
        public static readonly string InsufficientStorage = "Insufficient storage space. Please free up space and try again.";
        
        /// <summary>
        /// Error message for attempting to verify a checkpoint when too far away.
        /// </summary>
        public static readonly string CheckpointTooFar = "You must be within 50 feet of the checkpoint to verify.";
        
        /// <summary>
        /// Error message for attempting to verify an already verified checkpoint.
        /// </summary>
        public static readonly string CheckpointAlreadyVerified = "This checkpoint has already been verified.";
        
        /// <summary>
        /// Error message for failed checkpoint verification.
        /// </summary>
        public static readonly string CheckpointVerificationFailed = "Failed to verify checkpoint. Please try again.";
        
        /// <summary>
        /// Error message for actions requiring an active patrol when none exists.
        /// </summary>
        public static readonly string PatrolNotActive = "No active patrol. Please start a patrol first.";
        
        /// <summary>
        /// Error message for attempting to start a patrol when one is already active.
        /// </summary>
        public static readonly string PatrolAlreadyActive = "A patrol is already active. Please end the current patrol first.";
        
        /// <summary>
        /// Error message for when no checkpoints are found for a location.
        /// </summary>
        public static readonly string NoCheckpointsFound = "No checkpoints found for this location.";
        
        /// <summary>
        /// Error message for failed synchronization.
        /// </summary>
        public static readonly string SyncFailed = "Synchronization failed. Data will be synced when connection is available.";
        
        /// <summary>
        /// Message indicating the application is operating in offline mode.
        /// </summary>
        public static readonly string OfflineMode = "Operating in offline mode. Changes will be synced when connection is available.";
        
        /// <summary>
        /// Error message for invalid input.
        /// </summary>
        public static readonly string InvalidInput = "Invalid input provided. Please check and try again.";
        
        /// <summary>
        /// Error message for database errors.
        /// </summary>
        public static readonly string DatabaseError = "Database error occurred. Please restart the application.";
        
        /// <summary>
        /// Warning message for low battery conditions.
        /// </summary>
        public static readonly string LowBatteryWarning = "Low battery. Location tracking accuracy may be reduced to conserve power.";
    }
}