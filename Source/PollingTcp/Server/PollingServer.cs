using System;
using System.Collections.Concurrent;
using PollingTcp.Common;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Server
{
    public class PollingServer<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> 
        where TClientControlFrameType : ClientControlFrame
        where TClientDataFrameType : ClientDataFrame, ISequencedDataFrame
        where TServerDataFrameType : ServerDataFrame, new()
    {
        private readonly IServerNetworkLinkLayer networkLinkLayer;
        private readonly FrameEncoder<TClientDataFrameType> encoder;
        private readonly FrameEncoder<TServerDataFrameType> decoder;

        private readonly int maxIncomingSequenceValue;
        private readonly int maxOutgoingSequenceValue;
        private bool handleConnectionRequests;

        private ConcurrentBag<ClientSession<TClientDataFrameType, TServerDataFrameType>> clientSessions = new ConcurrentBag<ClientSession<TClientDataFrameType, TServerDataFrameType>>(); 

        public PollingServer(IServerNetworkLinkLayer networkLinkLayer, FrameEncoder<TClientControlFrameType> controlEncoder, FrameEncoder<TClientDataFrameType> encoder, FrameEncoder<TServerDataFrameType> decoder, int maxIncomingSequenceValue, int maxOutgoingSequenceValue)
        {
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

            return null;
        }
    }

    public class ClientSession<TClientDataFrameType, TServerDataFrameType>
        where TClientDataFrameType : ClientFrame, ISequencedDataFrame where TServerDataFrameType : ServerDataFrame, new()
    {
        private int clientId;
        private readonly int maxOutgoingSequenceNr;
        private readonly int maxIncomingSequenceNr;
        private int currentLocalSequenceNr;

        private object sequenceNrLock = new object();
        private FrameBuffer<TClientDataFrameType> frameBuffer;

        public int ClientId
        {
            get { return this.clientId; }
            set { this.clientId = value; }
        }

        public int CurrentLocalSequenceNr
        {
            get { return this.currentLocalSequenceNr; }
        }

        public ClientSession(int clientId, int maxIncomingSequenceNr, int maxOutgoingSequenceNr)
        {
            this.clientId = clientId;
            this.maxOutgoingSequenceNr = maxOutgoingSequenceNr;
            this.maxIncomingSequenceNr = maxIncomingSequenceNr;
            this.currentLocalSequenceNr = new Random((int)DateTime.Now.Ticks).Next(0, this.maxOutgoingSequenceNr);

            this.frameBuffer = new FrameBuffer<TClientDataFrameType>(maxIncomingSequenceNr);
        }

        public TServerDataFrameType CreateAcceptResponse()
        {
            var response = new TServerDataFrameType
            {
                Payload = BitConverter.GetBytes(this.ClientId)
            };

            return this.SetSequenceNr(response);
        }

        private TServerDataFrameType ProcessFrame(TClientDataFrameType clientFrame)
        {
            return null;
        }

        public TServerDataFrameType SetSequenceNr(TServerDataFrameType frame)
        {
            lock (this.sequenceNrLock)
            {
                frame.SequenceId = this.currentLocalSequenceNr;
                this.currentLocalSequenceNr = this.currentLocalSequenceNr < this.maxOutgoingSequenceNr ? this.currentLocalSequenceNr + 1 : 0;
            }

            return frame;
        }

        public TServerDataFrameType HandleClientFrame(ClientFrame clientFrame)
        {
            var pollingFrame = (ClientControlFrame) clientFrame;

            if (pollingFrame != null)
            {
                
            }

            return null;
        }
    }
}