using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Client
{
    public interface IClientFrameEncoder<in TClientControlFrameType, in TClientDataFrameType>
    {
        byte[] Encode(TClientControlFrameType controlFrame);
        byte[] Encode(TClientDataFrameType dataFrame);
        ClientFrame Decode(byte[] bytes);
    }
}