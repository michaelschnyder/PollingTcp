using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Server
{
    public class PollingTcpServer<TClientDataFrameType, TServerDataFrameType> where TClientDataFrameType : ClientDataFrame, new() where TServerDataFrameType : ServerDataFrame, new()
    {
        private readonly INetworkLinkLayer networkLinkLayer;
        private TransportLinkLayer<TServerDataFrameType, TClientDataFrameType> transportLayer;

        public PollingTcpServer(INetworkLinkLayer networkLinkLayer, FrameEncoder<TClientDataFrameType> encoder, FrameEncoder<TServerDataFrameType> decoder, int maxSequenceValue)
        {
            this.networkLinkLayer = networkLinkLayer;
            this.transportLayer = new TransportLinkLayer<TServerDataFrameType, TClientDataFrameType>(networkLinkLayer, decoder, encoder, maxSequenceValue);

            this.transportLayer.ProcessFrame = this.ProcessFrame;
        }

        private TServerDataFrameType ProcessFrame(TClientDataFrameType clientFrame)
        {
            // is this an connection attempt
            if (clientFrame.ClientId == 0)
            {
                
            }
            
            return new TServerDataFrameType();
        }

        public void Start()
        {

        }

        public ClientSession Accept()
        {
            return new ClientSession();
        }
    }

    public class ClientSession
    {
    }
}