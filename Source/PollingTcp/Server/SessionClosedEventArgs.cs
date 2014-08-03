using System;

namespace PollingTcp.Server
{
    public class SessionClosedEventArgs : EventArgs
    {
        public int ClientId { get; set; }

        public CloseReason Reason { get; set; }
    }
}