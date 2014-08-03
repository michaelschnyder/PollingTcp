using System;

namespace PollingTcp.Shared
{
    public interface IProtocolRuntimeSpefication
    {
        int MaxClientSequenceValue { get; }
        int MaxServerSequenceValue { get; }
        int MaxClientIdValue { get; }
        TimeSpan KeepAliveClientInterval { get; }
        TimeSpan KeepAliveServerInterval { get; }
    }
}