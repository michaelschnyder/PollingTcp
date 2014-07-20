using System;

namespace PollingTcp.Shared
{
    public class FrameReceivedEventArgs : EventArgs
    {
        public DataFrame[] Data { get; set; }
    }
}