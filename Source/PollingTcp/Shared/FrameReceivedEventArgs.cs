using System;

namespace PollingTcp.Shared
{
    public class FrameReceivedEventArgs<TDataFrameType> : EventArgs
    {
        public TDataFrameType[] Data { get; set; }
    }
}