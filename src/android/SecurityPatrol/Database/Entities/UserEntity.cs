using SQLite; // SQLite-net-pcl v1.8+
using System; // System v8.0+
using SecurityPatrol.Constants;

namespace SecurityPatrol.Database.Entities
{
    /// <summary>
    /// Represents a user in the SQLite database. This entity stores authentication information 
    /// including user ID, phone number, authentication token, and token expiry time.
    /// </summary>
    [Table(DatabaseConstants.TableUser)]
    public class UserEntity
    {
        /// <summary>
        /// Gets or sets the local database identifier.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique user identifier.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last successful authentication.
        /// </summary>
        public DateTime LastAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the authentication token for the user.
        /// </summary>
        public string AuthToken { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the authentication token.
        /// </summary>
        public DateTime TokenExpiry { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserEntity"/> class.
        /// </summary>
        public UserEntity()
        {
            Id = 0;
            UserId = string.Empty;
            PhoneNumber = string.Empty;
            LastAuthenticated = DateTime.MinValue;
            AuthToken = string.Empty;
            TokenExpiry = DateTime.MinValue;
        }
    }
}