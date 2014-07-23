using System;

namespace PollingTcp.Shared
{
    public class FrameBlockReceivedEventArgs<TDataFrameType> : EventArgs
    {
        public TDataFrameType[] Data { get; set; }
        public object ReturnValue { get; set; }
    }
}