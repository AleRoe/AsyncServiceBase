using System;

namespace AsyncService
{
    public class AsyncServiceFaultedEventArgs : EventArgs
    {
        public Exception Fault { get; }

        public AsyncServiceFaultedEventArgs(Exception exception)
        {
            this.Fault = exception;
        }
    }
}