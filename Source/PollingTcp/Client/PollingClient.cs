using System;
using System.Threading;
using System.Threading.Tasks;
using PollingTcp.Common;
using PollingTcp.Frame;

namespace PollingTcp.Client
{
    public class PollingClient<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> : IDisposable
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, new() 
        where TServerDataFrameType : ServerDataFrame
    {
        private ConnectionState connectionState;

        private readonly IClientNetworkLinkLayer networkLinkLayer;
        private readonly IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> clientEncoder;

        private readonly ClientTransportLayer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> transportLayer;
        private int clientId;

        private readonly AutoResetEvent connectedEvent = new AutoResetEvent(false);
        private TimeSpan connectionEstablishTimeout = TimeSpan.FromMilliseconds(5000);


        private bool expectConnectionEstablishement = false;
        private RequestPool<TClientControlFrameType> requestPool;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        public event EventHandler<FrameReceivedEventArgs<TServerDataFrameType>> FrameReceived;

        protected virtual void OnFrameReceived(FrameReceivedEventArgs<TServerDataFrameType> e)
        {
            EventHandler<FrameReceivedEventArgs<TServerDataFrameType>> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        public TimeSpan ConnectionEstablishTimeout
        {
            get { return this.connectionEstablishTimeout; }
            set { this.connectionEstablishTimeout = value; }
        }

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

            this.requestPool = new RequestPool<TClientControlFrameType>(this.transportLayer);
        }

        private void TransportLayerOnFrameReceived(object sender, FrameReceivedEventArgs<TServerDataFrameType> frameReceivedEventArgs)
        {
            if (this.connectionState == ConnectionState.Connecting && this.expectConnectionEstablishement)
            {
                var data = frameReceivedEventArgs.Frame.Payload;

                this.clientId = BitConverter.ToInt32(data, 0);

                connectedEvent.Set();
                this.SetNewConnectionState(ConnectionState.Connected);

                this.requestPool.ClientId = this.clientId;
                this.requestPool.Start();

                // Todo: Start timeout watcher
            }
            else if (this.connectionState == ConnectionState.Connected)
            {
                this.OnFrameReceived(new FrameReceivedEventArgs<TServerDataFrameType>
                {
                    Frame = frameReceivedEventArgs.Frame
                });
            }
        }

        public ConnectionState ConnectionState
        {
            get { return this.connectionState; }
        }

        public Task<bool> ConnectAsync()
        {
            if (this.connectionState != ConnectionState.Disconnected)
            {
                throw new Exception("Cannot start connection when not beeing in Disconnected-State");    
            }

            SetNewConnectionState(ConnectionState.Connecting);

            this.expectConnectionEstablishement = true;

            // Send an empty DataFrame to initiate the connection
            this.transportLayer.Send(new TClientDataFrameType());

            var ensureConnectedWithinTimeout = new Task<bool>(() =>
            {
                this.connectedEvent.WaitOne(this.ConnectionEstablishTimeout);

                this.expectConnectionEstablishement = false;

                if (this.connectionState != ConnectionState.Connected)
                {
                    this.SetNewConnectionState(ConnectionState.Disconnected);
                }

                return this.ConnectionState == ConnectionState.Connected;
            });

            ensureConnectedWithinTimeout.Start();

            return ensureConnectedWithinTimeout;
        }

        public Task DisconnectAsync()
        {
            if (this.connectionState != ConnectionState.Connected && this.connectionState != ConnectionState.Connecting)
            {
                throw new Exception(string.Format("Illegal State to disconnect: {0}", this.connectionState));
            }
            
            var afterPoolShutdown = new Action<Task>((o) =>
            {
                this.connectionState = ConnectionState.Disconnected;
            });

            return this.requestPool.StopAsync().ContinueWith(afterPoolShutdown);
        }

        public void Send(TClientDataFrameType frame)
        {
            if (this.ConnectionState != ConnectionState.Connected)
            {
                throw new Exception("Illegal State. Not connected to server");
            }

            frame.ClientId = this.clientId;
            this.transportLayer.Send(frame);
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

        protected virtual void Dispose(bool forceAll)
        {
            this.requestPool.StopAsync();
            this.requestPool = null;
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }
    }
}
