using System;

namespace AsyncService
{
    public class AsyncServiceStatusChangedEventArgs : EventArgs
    {
        public AsyncServiceStatus FromStatus { get;}
        public AsyncServiceStatus ToStatus { get; }

        public AsyncServiceStatusChangedEventArgs(AsyncServiceStatus from, AsyncServiceStatus to)
        {
            this.FromStatus = from;
            this.ToStatus = to;
        }
    }
}