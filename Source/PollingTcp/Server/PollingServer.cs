using System;
using System.Collections.Concurrent;
using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Server
{
    public class PollingServer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> 
        where TClientControlFrameType : ClientControlFrame
        where TClientDataFrameType : ClientDataFrame, ISequencedDataFrame
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

        public PollingServer(IServerNetworkLinkLayer networkLinkLayer, IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> encoder, FrameEncoder<TServerDataFrameType> decoder, int maxIncomingSequenceValue, int maxOutgoingSequenceValue)
        {
            if (networkLinkLayer == null)
            {
                throw new Exception("Network Link Layer is not set!");
            }

            this.networkLinkLayer = networkLinkLayer;
            this.encoder = encoder;
            this.decoder = decoder;
            this.maxOutgoingSequenceValue = maxOutgoingSequenceValue;
            this.maxIncomingSequenceValue = maxIncomingSequenceValue;

            this.networkLinkLayer.PollHandler = this.NetworkLayerPollHandler;
        }

        private byte[] NetworkLayerPollHandler(byte[] arg)
        {
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
            else
            {
                return null;
            }
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

        }

        public ClientSession<TClientDataFrameType, TServerDataFrameType> Accept()
        {
            this.handleConnectionRequests = true;

            ClientSession<TClientDataFrameType, TServerDataFrameType> clientSession = null;

            while (clientSession == null)
            {
                this.connectionRequests.TryTake(out clientSession, 1000);
            }

            return clientSession;
        }
    }
}