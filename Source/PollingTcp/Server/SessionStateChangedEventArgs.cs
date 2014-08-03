using System;

namespace PollingTcp.Server
{
    public class SessionStateChangedEventArgs : EventArgs
    {
        public SessionState PreviousState { get; set; }
        public SessionState State { get; set; }
    }
}