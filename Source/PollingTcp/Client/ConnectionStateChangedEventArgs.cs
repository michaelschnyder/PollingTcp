using System;

namespace PollingTcp.Client
{
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
        public ConnectionState PreviousState { get; set; }
    }
}