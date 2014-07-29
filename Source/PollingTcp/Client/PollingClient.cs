using System;
using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Client
{
    public class PollingClient<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, new() 
        where TServerDataFrameType : ServerDataFrame
    {
        private ConnectionState connectionState;

        private readonly IClientNetworkLinkLayer networkLinkLayer;
        private readonly IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> clientEncoder;
        private readonly FrameEncoder<TClientDataFrameType> dataEncoder;

        private readonly ClientTransportLayer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> transportLayer;
        private int clientId;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        protected virtual void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            EventHandler<ConnectionStateChangedEventArgs> handler = this.ConnectionStateChanged;
            if (handler != null) handler(this, e);
        }

        public PollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> clientEncoder, FrameEncoder<TServerDataFrameType> serverDecoder, int maxSequenceValue)
        {
            if (clientNetworkLinkLayer == null)
            {
                throw new Exception("Logical Link Layer is not set!");
            }

            this.networkLinkLayer = clientNetworkLinkLayer;
            this.clientEncoder = clientEncoder;
            this.transportLayer = new ClientTransportLayer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>(clientNetworkLinkLayer, clientEncoder, serverDecoder, maxSequenceValue);

            this.transportLayer.FrameReceived += TransportLayerOnFrameReceived;
        }

        private void TransportLayerOnFrameReceived(object sender, FrameReceivedEventArgs<TServerDataFrameType> frameReceivedEventArgs)
        {
            if (this.connectionState == ConnectionState.Connecting)
            {
                var data = frameReceivedEventArgs.Frame.Payload;

                this.clientId = BitConverter.ToInt32(data, 0);

                this.SetNewConnectionState(ConnectionState.Connected);
            }
        }

        public ConnectionState ConnectionState
        {
            get { return this.connectionState; }
        }

        public void Connect()
        {
            if (this.connectionState != ConnectionState.Disconnected)
            {
                throw new Exception("Cannot start connection when not beeing in Disconnected-State");    
            }

            SetNewConnectionState(ConnectionState.Connecting);

            // Send an empty DataFrame to initiate the connection
            this.transportLayer.Send(new TClientDataFrameType());
        }

        public void Send(TClientDataFrameType frame)
        {
            if (this.ConnectionState != ConnectionState.Connected)
            {
                frame.ClientId = this.clientId;
                this.transportLayer.Send(frame);
            }
        }

        private void SetNewConnectionState(ConnectionState state)
        {
            if (this.connectionState != state)
            {
                var oldState = this.connectionState;
                this.connectionState = state;

                this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs()
                {
                    PreviousState = oldState,
                    State = this.connectionState
                });
            }
        }
    }
}
