using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Tests.Helper
{
    class BinaryClientFrameEncoder : FrameEncoder<ClientFrame>
    {
        GenericSerializer<ClientFrame> serializer = new GenericSerializer<ClientFrame>(); 

        public override ClientFrame Decode(byte[] bytes)
        {
            return this.serializer.Deserialze(bytes);
        }

        public override byte[] Encode(ClientFrame clientFrame)
        {
            return this.serializer.Serialize(clientFrame);
        }
    }
}