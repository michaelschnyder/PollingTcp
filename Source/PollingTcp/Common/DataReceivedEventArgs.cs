using System;

namespace PollingTcp.Common
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
    }
}