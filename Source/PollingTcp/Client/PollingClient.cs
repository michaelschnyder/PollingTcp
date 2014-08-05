using System;
using System.Threading;
using System.Threading.Tasks;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Client
{
    public class PollingClient<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> : IDisposable
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, new() 
        where TServerDataFrameType : ServerDataFrame, new()
    {
        private readonly ClientTransportLayer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> transportLayer;
        private readonly IProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> protocolSpecification;

        private ConnectionState connectionState;

        private int clientId;

        private readonly AutoResetEvent connectedEvent = new AutoResetEvent(false);
        private TimeSpan connectionTimeout = TimeSpan.FromMilliseconds(1000);
        private TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(1000);

        private bool expectConnectionEstablishement;

        private RequestPool<TClientControlFrameType> requestPool;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        public event EventHandler<FrameReceivedEventArgs<TServerDataFrameType>> FrameReceived;

        private readonly Timer receivedDataTimeoutTimer;

        private readonly Timer sendKeepAlivePacketTimer;
        
        private int initialPollingPoolSize;

        #region Event Invocators

        protected virtual void OnFrameReceived(FrameReceivedEventArgs<TServerDataFrameType> e)
        {
            EventHandler<FrameReceivedEventArgs<TServerDataFrameType>> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            EventHandler<ConnectionStateChangedEventArgs> handler = this.ConnectionStateChanged;
            if (handler != null) handler(this, e);
        }


        #endregion

        #region Properties

        public TimeSpan ConnectionTimeout
        {
            get { return this.connectionTimeout; }
            set { this.connectionTimeout = value; }
        }

        public TimeSpan ReceiveTimeout
        {
            get { return this.receiveTimeout; }
            set { this.receiveTimeout = value; }
        }

        public IProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> Protocol
        {
            get { return this.protocolSpecification; }
        }

        public ConnectionState ConnectionState
        {
            get { return this.connectionState; }
        }

        public int InitialPollingPoolSize
        {
            get { return this.initialPollingPoolSize; }
            set { this.initialPollingPoolSize = value; }
        }

        public int CurrentPollingPoolSize {
            get { return this.requestPool.ActiveClients; }
        }

        #endregion

        #region Constructor

        public PollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> specification)
        {
            if (clientNetworkLinkLayer == null)
            {
                throw new ArgumentNullException("clientNetworkLinkLayer", "Logical Link Layer is not set!");
            }

            if (specification == null)
            {
                throw new ArgumentNullException("specification", "Protocol-Specification cannot be null!");
            }

            if (specification.ClientEncoder == null)
            {
                throw new ArgumentException("specification", string.Format("ClientEncoder must be provided for given specification '{0}'", specification.GetType().Name));
            }

            if (specification.ServerEncoder == null)
            {
                throw new ArgumentException("specification", string.Format("ServerEncoder must be provided for given specification '{0}'", specification.GetType().Name));
            }

            this.protocolSpecification = specification;

            this.transportLayer = new ClientTransportLayer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>(
                clientNetworkLinkLayer, 
                specification.ClientEncoder, 
                specification.ServerEncoder, 
                specification.MaxClientSequenceValue,
                specification.MaxServerSequenceValue);

            this.transportLayer.FrameReceived += TransportLayerOnFrameReceived;

            this.InitialPollingPoolSize = 4;

            this.receivedDataTimeoutTimer = new Timer(this.ReceivedDataTimeoutTimerCallback);
            this.sendKeepAlivePacketTimer = new Timer(SendKeepAlivePacketTimerCallback);
        }

        public PollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> clientEncoder, FrameEncoder<TServerDataFrameType> serverEncoder)
            : this(clientNetworkLinkLayer, new DefaultProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>() {ClientEncoder = clientEncoder, ServerEncoder = serverEncoder})
        {
        }

        public PollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> clientEncoder, FrameEncoder<TServerDataFrameType> serverEncoder, int maxClientSequenceValue,
            int maxServerSequenceValue)
            : this(
                clientNetworkLinkLayer,
                new DefaultProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>()
                {
                    ClientEncoder = clientEncoder,
                    ServerEncoder = serverEncoder,
                    MaxClientSequenceValue = maxClientSequenceValue,
                    MaxServerSequenceValue = maxServerSequenceValue
                })
        {
        }

        #endregion

        private void ReceivedDataTimeoutTimerCallback(object state)
        {
            this.SetNewConnectionState(ConnectionState.Timeout);
            this.ShutdownTimers();

            this.requestPool.StopAsync().Wait();

            this.SetNewConnectionState(ConnectionState.Disconnected);
        }

        private void SendKeepAlivePacketTimerCallback(object state)
        {
            this.transportLayer.Send(new TClientControlFrameType()
            {
                ClientId = this.clientId,
            });
        }

        private void TransportLayerOnFrameReceived(object sender, FrameReceivedEventArgs<TServerDataFrameType> frameReceivedEventArgs)
        {
            this.receivedDataTimeoutTimer.Change(this.receiveTimeout, Timeout.InfiniteTimeSpan);

            if (this.connectionState == ConnectionState.Connecting && this.expectConnectionEstablishement)
            {
                var data = frameReceivedEventArgs.Frame.Payload;

                var assignedClientId = BitConverter.ToInt32(data, 0);

                this.StartConnection(assignedClientId);
            }
            else if (this.connectionState == ConnectionState.Connected)
            {
                this.OnFrameReceived(new FrameReceivedEventArgs<TServerDataFrameType>
                {
                    Frame = frameReceivedEventArgs.Frame
                });
            }
        }

        private void StartConnection(int assignedClientId)
        {
            this.clientId = assignedClientId;

            this.connectedEvent.Set();
            this.SetNewConnectionState(ConnectionState.Connected);

            this.requestPool = new RequestPool<TClientControlFrameType>(this.transportLayer, this.InitialPollingPoolSize);

            this.requestPool.ClientId = this.clientId;
            this.requestPool.Start();

            this.receivedDataTimeoutTimer.Change(this.receiveTimeout, Timeout.InfiniteTimeSpan);
            this.sendKeepAlivePacketTimer.Change(this.protocolSpecification.KeepAliveClientInterval, this.protocolSpecification.KeepAliveClientInterval);
        }

        private void ShutdownTimers()
        {
            this.receivedDataTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.sendKeepAlivePacketTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public Task<bool> ConnectAsync()
        {
            if (this.connectionState != ConnectionState.Disconnected)
            {
                throw new Exception(string.Format("Cannot start connection when not beeing in '{0}'-state", this.connectionState));    
            }

            SetNewConnectionState(ConnectionState.Connecting);

            this.expectConnectionEstablishement = true;

            // Send an empty DataFrame to initiate the connection
            this.transportLayer.Send(new TClientDataFrameType());

            var ensureConnectedWithinTimeout = new Task<bool>(() =>
            {
                this.connectedEvent.WaitOne(this.ConnectionTimeout);
                this.connectedEvent.Reset();

                this.expectConnectionEstablishement = false;

                if (this.connectionState != ConnectionState.Connected)
                {
                    this.SetNewConnectionState(ConnectionState.Timeout);
                    this.SetNewConnectionState(ConnectionState.Disconnected);
                }

                return this.ConnectionState == ConnectionState.Connected;
            });

            ensureConnectedWithinTimeout.Start();

            return ensureConnectedWithinTimeout;
        }

        public Task DisconnectAsync()
        {
            this.ShutdownTimers();

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
