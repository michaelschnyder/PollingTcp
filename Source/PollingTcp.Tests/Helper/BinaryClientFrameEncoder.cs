using PollingTcp.Client;
using PollingTcp.Shared;

namespace PollingTcp.Tests.Helper
{
    class BinaryClientFrameEncoder : IClientFrameEncoder<ClientControlFrame, ClientDataFrame>
    {
        GenericSerializer<ClientFrame> serializer = new GenericSerializer<ClientFrame>(); 

        public ClientFrame Decode(byte[] bytes)
        {
            return this.serializer.Deserialze(bytes);
        }

        public byte[] Encode(ClientControlFrame controlFrame)
        {
            return this.serializer.Serialize(controlFrame);
        }

        public byte[] Encode(ClientDataFrame dataFrame)
        {
            return this.serializer.Serialize(dataFrame);
        }
    }
}