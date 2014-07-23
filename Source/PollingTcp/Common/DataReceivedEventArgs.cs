using System;

namespace PollingTcp.Common
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Bytes { get; set; }
        public byte[] Response { get; set; }
    }
}