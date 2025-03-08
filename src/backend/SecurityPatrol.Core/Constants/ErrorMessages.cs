namespace SecurityPatrol.Core.Constants
{
    /// <summary>
    /// Static class containing constants for error messages used throughout the Security Patrol application.
    /// These provide standardized error messages for consistent error reporting.
    /// </summary>
    public static class ErrorMessages
    {
        #region General Errors

        /// <summary>
        /// Generic error message for internal server errors.
        /// </summary>
        public const string General_InternalServerError = "An unexpected error occurred. Please try again later.";

        /// <summary>
        /// Generic error message for resource not found errors.
        /// </summary>
        public const string General_NotFound = "The requested resource was not found.";

        /// <summary>
        /// Generic error message for unauthorized access.
        /// </summary>
        public const string General_Unauthorized = "You are not authorized to perform this action.";

        #endregion

        #region Validation Errors

        /// <summary>
        /// Error message for required field validation failures.
        /// </summary>
        public const string Validation_Required = "The {0} field is required.";

        /// <summary>
        /// Error message for format validation failures.
        /// </summary>
        public const string Validation_InvalidFormat = "The {0} field has an invalid format.";

        #endregion

        #region Authentication Errors

        /// <summary>
        /// Error message for invalid phone number format.
        /// </summary>
        public const string Auth_InvalidPhoneNumber = "Please enter a valid phone number with country code.";

        /// <summary>
        /// Error message for invalid verification code.
        /// </summary>
        public const string Auth_InvalidVerificationCode = "The verification code is invalid.";

        /// <summary>
        /// Error message for expired verification code.
        /// </summary>
        public const string Auth_VerificationCodeExpired = "The verification code has expired. Please request a new code.";

        /// <summary>
        /// Error message for expired authentication token.
        /// </summary>
        public const string Auth_TokenExpired = "Your session has expired. Please log in again.";

        /// <summary>
        /// Error message for invalid authentication token.
        /// </summary>
        public const string Auth_InvalidToken = "Invalid authentication token.";

        #endregion

        #region User Errors

        /// <summary>
        /// Error message for user not found.
        /// </summary>
        public const string User_NotFound = "User not found.";

        #endregion

        #region Time Record Errors

        /// <summary>
        /// Error message for invalid time record type.
        /// </summary>
        public const string TimeRecord_InvalidType = "Invalid time record type. Must be 'ClockIn' or 'ClockOut'.";

        /// <summary>
        /// Error message for attempting to clock in when already clocked in.
        /// </summary>
        public const string TimeRecord_AlreadyClockedIn = "You are already clocked in.";

        /// <summary>
        /// Error message for attempting to clock out when already clocked out.
        /// </summary>
        public const string TimeRecord_AlreadyClockedOut = "You are already clocked out.";

        /// <summary>
        /// Error message for time record not found.
        /// </summary>
        public const string TimeRecord_NotFound = "Time record not found.";

        #endregion

        #region Location Errors

        /// <summary>
        /// Error message for invalid location coordinates.
        /// </summary>
        public const string Location_InvalidCoordinates = "Invalid location coordinates.";

        /// <summary>
        /// Error message for empty location batch.
        /// </summary>
        public const string Location_BatchEmpty = "Location batch cannot be empty.";

        #endregion

        #region Patrol Errors

        /// <summary>
        /// Error message for patrol location not found.
        /// </summary>
        public const string Patrol_LocationNotFound = "Patrol location not found.";

        /// <summary>
        /// Error message for checkpoint not found.
        /// </summary>
        public const string Patrol_CheckpointNotFound = "Checkpoint not found.";

        /// <summary>
        /// Error message for attempting to verify an already verified checkpoint.
        /// </summary>
        public const string Patrol_CheckpointAlreadyVerified = "Checkpoint already verified.";

        /// <summary>
        /// Error message for attempting to verify a checkpoint when not in proximity.
        /// </summary>
        public const string Patrol_NotInProximity = "You must be within 50 feet of the checkpoint to verify it.";

        #endregion

        #region Photo Errors

        /// <summary>
        /// Error message for invalid photo format.
        /// </summary>
        public const string Photo_InvalidFormat = "Invalid photo format. Supported formats are JPEG and PNG.";

        /// <summary>
        /// Error message for photo exceeding size limit.
        /// </summary>
        public const string Photo_TooLarge = "Photo size exceeds the maximum allowed size.";

        /// <summary>
        /// Error message for photo upload failure.
        /// </summary>
        public const string Photo_UploadFailed = "Failed to upload photo. Please try again.";

        /// <summary>
        /// Error message for photo not found.
        /// </summary>
        public const string Photo_NotFound = "Photo not found.";

        #endregion

        #region Report Errors

        /// <summary>
        /// Error message for report text exceeding maximum length.
        /// </summary>
        public const string Report_TextTooLong = "Report text exceeds maximum length of 500 characters.";

        /// <summary>
        /// Error message for empty report text.
        /// </summary>
        public const string Report_TextRequired = "Report text cannot be empty.";

        /// <summary>
        /// Error message for report not found.
        /// </summary>
        public const string Report_NotFound = "Report not found.";

        #endregion
    }
}