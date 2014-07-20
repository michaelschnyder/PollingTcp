using System;
using PollingTcp.Common;

namespace PollingTcp.Client
{
    public class PollingTcpClient
    {
        private ConnectionState connectionState;

        private readonly ILogicalLinkLayer logicalLinkLayer;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        protected virtual void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            EventHandler<ConnectionStateChangedEventArgs> handler = this.ConnectionStateChanged;
            if (handler != null) handler(this, e);
        }

        public PollingTcpClient(ILogicalLinkLayer logicalLinkLayer)
        {
            this.logicalLinkLayer = logicalLinkLayer;
        }

        public ConnectionState ConnectionState
        {
            get { return this.connectionState; }
        }

        public void Connect()
        {
            if (this.logicalLinkLayer == null)
            {
                throw new Exception("Logical Link Layer is not set!");
            }

            if (this.connectionState != ConnectionState.Disconnected)
            {
                throw new Exception("Cannot start connection when not beeing in Disconnected-State");    
            }

            SetNewConnectionState(ConnectionState.Connecting);
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
