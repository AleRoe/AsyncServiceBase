using System;

namespace AsyncService
{
    /// <summary>
    /// Provides <see cref="AsyncServiceStatus"/> data when a <see cref="AsyncServiceBase"/> service status changes.
    /// </summary>
    public class AsyncServiceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous state of the service
        /// </summary>
        public AsyncServiceStatus FromStatus { get;}

        /// <summary>
        /// Gets the new state of the service
        /// </summary>
        public AsyncServiceStatus ToStatus { get; }

        /// <summary>
        /// Initializes a new <see cref="AsyncServiceStatusChangedEventArgs"/> instance.
        /// </summary>
        /// <param name="from">The previous <see cref="AsyncServiceStatus"/> state of the service</param>
        /// <param name="to">The new <see cref="AsyncServiceStatus"/> state of the service</param>
        public AsyncServiceStatusChangedEventArgs(AsyncServiceStatus from, AsyncServiceStatus to)
        {
            this.FromStatus = from;
            this.ToStatus = to;
        }
    }
}