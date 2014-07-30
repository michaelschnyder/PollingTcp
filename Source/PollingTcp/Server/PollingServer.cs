using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Server
{
    public class PollingServer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> : IDisposable
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, ISequencedDataFrame, new()
        where TServerDataFrameType : ServerDataFrame, new()
    {
        private readonly IServerNetworkLinkLayer networkLinkLayer;
        private readonly IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> encoder;
        private readonly FrameEncoder<TServerDataFrameType> decoder;

        private readonly int maxIncomingSequenceValue;
        private readonly int maxOutgoingSequenceValue;
        private bool handleConnectionRequests;

        private readonly ConcurrentBag<ClientSession<TClientDataFrameType, TServerDataFrameType>> clientSessions = new ConcurrentBag<ClientSession<TClientDataFrameType, TServerDataFrameType>>(); 

        private readonly BlockingCollection<ClientSession<TClientDataFrameType, TServerDataFrameType>> connectionRequests = new BlockingCollection<ClientSession<TClientDataFrameType, TServerDataFrameType>>();
        private CancellationTokenSource cancellationToken;
        private bool isStarted;

        private IProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> protocolSpecification;

        public int SessionCount
        {
            get { return this.clientSessions.Count; }
        }

        public PollingServer(IServerNetworkLinkLayer networkLinkLayer, IProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> specification)
        {
            if (networkLinkLayer == null)
            {
                throw new ArgumentNullException("networkLinkLayer", "Logical Link Layer is not set!");
            }

            if (specification == null)
            {
                throw new ArgumentNullException("specification", "Protocol-Specification cannot be null!");
            }

            if (specification.ClientEncoder == null)
            {
                throw new ArgumentException("specification", string.Format("ClientEncoder must be provided for given ProtocolSpecification '{0}'", specification.GetType().Name));
            }

            if (specification.ServerEncoder == null)
            {
                throw new ArgumentException("specification", string.Format("ServerEncoder must be provided for given ProtocolSpecification '{0}'", specification.GetType().Name));
            }

            this.protocolSpecification = specification;

            this.networkLinkLayer = networkLinkLayer;
            this.encoder = specification.ClientEncoder;
            this.decoder = specification.ServerEncoder;

            this.maxOutgoingSequenceValue = specification.MaxServerSequenceValue;
            this.maxIncomingSequenceValue = specification.MaxClientSequenceValue;

            this.networkLinkLayer.PollHandler = this.NetworkLayerPollHandler;
        }

        public PollingServer(IServerNetworkLinkLayer networkLinkLayer, IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> encoder, FrameEncoder<TServerDataFrameType> decoder, int maxIncomingSequenceValue, int maxOutgoingSequenceValue)
         : this(networkLinkLayer, new DefaultProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>() { ClientEncoder = encoder, ServerEncoder = decoder, MaxClientSequenceValue = maxIncomingSequenceValue, MaxServerSequenceValue = maxOutgoingSequenceValue})
        {
        }

        private byte[] NetworkLayerPollHandler(byte[] arg)
        {
            if (!this.isStarted)
            {
                return null;
            }

            var clientFrame = this.encoder.Decode(arg);
            TServerDataFrameType response = null;

            if (clientFrame.ClientId == 0)
            {
                if (this.handleConnectionRequests)
                {
                    response = this.CreateNewClientSession();
                }
            }
            else
            {
                ClientSession<TClientDataFrameType, TServerDataFrameType> clientSession;
                if (this.clientSessions.TryPeek(out clientSession))
                {
                    response = clientSession.HandleClientFrame(clientFrame);
                }
            }
            
            if (response != null)
            {
                return this.decoder.Encode(response);
            }
            
            return null;
        }

        private TServerDataFrameType CreateNewClientSession()
        {
            var clientId = this.GetClientSession();

            var clientSession = new ClientSession<TClientDataFrameType, TServerDataFrameType>(clientId, this.maxIncomingSequenceValue, this.maxOutgoingSequenceValue);
            this.clientSessions.Add(clientSession);

            this.connectionRequests.Add(clientSession);

            var acceptResponse = clientSession.CreateAcceptResponse();
            return acceptResponse;
        }

        private int GetClientSession()
        {
            return new Random().Next(12, 849849);
        }

        public void Start()
        {
            this.isStarted = true;
            this.cancellationToken = new CancellationTokenSource();
        }

        public void Stop()
        {
            this.cancellationToken.Cancel(false);

            this.handleConnectionRequests = false;

            this.isStarted = false;
        }

        public ClientSession<TClientDataFrameType, TServerDataFrameType> Accept()
        {
            if (!this.isStarted)
            {
                throw new Exception("Server has to be started first!");
            }

            this.handleConnectionRequests = true;

            ClientSession<TClientDataFrameType, TServerDataFrameType> clientSession = null;

            while (clientSession == null && !this.cancellationToken.IsCancellationRequested)
            {
                this.connectionRequests.TryTake(out clientSession, 1000);
            }

            return clientSession;
        }

        public Task<ClientSession<TClientDataFrameType, TServerDataFrameType>> AcceptAsync()
        {
            Task<ClientSession<TClientDataFrameType, TServerDataFrameType>> task;
                
            task = new Task<ClientSession<TClientDataFrameType, TServerDataFrameType>>(this.Accept);

            task.Start();

            return task;
        }

        protected virtual void Dispose(bool cleanupBothManagedAndNativeResources)
        {
            this.connectionRequests.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}