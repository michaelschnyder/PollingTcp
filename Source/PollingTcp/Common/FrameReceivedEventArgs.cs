using System;

namespace PollingTcp.Common
{
    public class FrameReceivedEventArgs<TFrameDataType> : EventArgs
    {
        public TFrameDataType Frame { get; set; }
    }
}