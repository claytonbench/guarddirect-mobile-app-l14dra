using System;

namespace SecurityPatrol.Models
{
    /// <summary>
    /// Event arguments class that provides data for checkpoint proximity events, indicating when a user 
    /// enters or exits the defined proximity radius of a checkpoint.
    /// </summary>
    public class CheckpointProximityEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier of the checkpoint.
        /// </summary>
        public int CheckpointId { get; }

        /// <summary>
        /// Gets the current distance to the checkpoint in feet.
        /// </summary>
        public double Distance { get; }

        /// <summary>
        /// Gets a value indicating whether the user is within the defined proximity radius of the checkpoint.
        /// Typically, this is true when the user is within 50 feet of the checkpoint.
        /// </summary>
        public bool IsInRange { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointProximityEventArgs"/> class with the 
        /// specified checkpoint ID, distance, and proximity status.
        /// </summary>
        /// <param name="checkpointId">The unique identifier of the checkpoint.</param>
        /// <param name="distance">The current distance to the checkpoint in feet.</param>
        /// <param name="isInRange">A value indicating whether the user is within the defined proximity radius of the checkpoint.</param>
        public CheckpointProximityEventArgs(int checkpointId, double distance, bool isInRange)
        {
            CheckpointId = checkpointId;
            Distance = distance;
            IsInRange = isInRange;
        }
    }
}