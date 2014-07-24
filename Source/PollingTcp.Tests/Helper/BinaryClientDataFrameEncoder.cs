using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Tests.Helper
{
    class BinaryClientDataFrameEncoder : FrameEncoder<ClientDataFrame>
    {
        GenericSerializer<ClientDataFrame> serializer = new GenericSerializer<ClientDataFrame>(); 

        public override ClientDataFrame Decode(byte[] bytes)
        {
            return this.serializer.Deserialze(bytes);
        }

        public override byte[] Encode(ClientDataFrame clientFrame)
        {
            return this.serializer.Serialize(clientFrame);
        }
    }
}