using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Tests.Helper
{
    class BinaryClientControlFrameEncoder : FrameEncoder<ClientControlFrame>
    {
        GenericSerializer<ClientControlFrame> serializer = new GenericSerializer<ClientControlFrame>();

        public override ClientControlFrame Decode(byte[] bytes)
        {
            return this.serializer.Deserialze(bytes);
        }

        public override byte[] Encode(ClientControlFrame clientFrame)
        {
            return this.serializer.Serialize(clientFrame);
        }
    }
}