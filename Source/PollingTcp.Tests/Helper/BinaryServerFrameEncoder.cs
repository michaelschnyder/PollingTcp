using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Tests
{
    class BinaryServerFrameEncoder : FrameEncoder<ServerDataFrame>
    {
        GenericSerializer<ServerDataFrame> serializer = new GenericSerializer<ServerDataFrame>(); 

        public override ServerDataFrame Decode(byte[] bytes)
        {
            return this.serializer.Deserialze(bytes);
        }

        public override byte[] Encode(ServerDataFrame clientFrame)
        {
            return this.serializer.Serialize(clientFrame);
        }
    }
}