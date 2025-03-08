namespace SecurityPatrol.Core.Constants
{
    /// <summary>
    /// Defines constant string values for JWT claim types used in the Security Patrol application.
    /// These constants are used for creating and validating JWT tokens throughout the authentication system.
    /// </summary>
    public static class ClaimTypes
    {
        /// <summary>
        /// Claim type for the user's unique identifier.
        /// </summary>
        public const string UserId = "security_patrol_user_id";

        /// <summary>
        /// Claim type for the user's phone number.
        /// </summary>
        public const string PhoneNumber = "security_patrol_phone_number";

        /// <summary>
        /// Claim type for the user's role in the system.
        /// </summary>
        public const string Role = "security_patrol_role";
    }
}