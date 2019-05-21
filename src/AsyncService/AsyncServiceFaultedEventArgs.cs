using System;

namespace AsyncService
{
    /// <summary>
    /// Provides information on the <see cref="Exception"/> which caused a <see cref="AsyncServiceBase"/> to fault.
    /// </summary>
    public class AsyncServiceFaultedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="Exception"/> which caused the service to fault.
        /// </summary>
        public Exception Fault { get; }

        /// <summary>
        /// Initializes a new <see cref="AsyncServiceFaultedEventArgs"/> instance.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> which caused the service to fault.</param>
        public AsyncServiceFaultedEventArgs(Exception exception)
        {
            this.Fault = exception;
        }
    }
}