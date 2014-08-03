using PollingTcp.Client;
using PollingTcp.Frame;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    class ClientTestTransportLayer : ISendControlFrame<ClientControlFrame>
    {
        private readonly ClientTestNetworkLinkLayer clientNetworkLayer;
        private BinaryClientFrameEncoder encoder;

        public ClientTestTransportLayer(ClientTestNetworkLinkLayer clientNetworkLayer)
        {
            this.clientNetworkLayer = clientNetworkLayer;
            this.encoder = new BinaryClientFrameEncoder();
        }

        public void Send(ClientControlFrame sendFrame)
        {
            this.clientNetworkLayer.Send(this.encoder.Encode(sendFrame));
        }
    }
}