namespace SecurityPatrol.Core.Constants
{
    /// <summary>
    /// Static class containing constants for user roles used in the Security Patrol application.
    /// These constants are used for role-based access control and JWT token claims.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Role for security personnel users who are the primary users of the application.
        /// </summary>
        public const string SecurityPersonnel = "security_personnel";
    }
}