using System;

namespace PollingTcp.Common
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Bytes { get; set; }
    }
}