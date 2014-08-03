using System;
using System.Collections.Concurrent;
using System.Threading;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Server
{
    public class PollingClientSession<TClientDataFrameType, TServerDataFrameType> : IDisposable
        where TClientDataFrameType : ClientFrame, ISequencedDataFrame,new() 
        where TServerDataFrameType : ServerDataFrame, new()
    {
        private readonly int maxOutgoingSequenceNr;
        private readonly int maxIncomingSequenceNr;

        private readonly int clientId;
        private readonly IProtocolRuntimeSpefication protocolruntimeSpecification;

        private int currentLocalSequenceNr;

        private SessionState sessionState = SessionState.Created;

        private readonly object sequenceNrLock = new object();
        private readonly FrameBuffer<TClientDataFrameType> frameBuffer;
        private readonly ConcurrentQueue<TServerDataFrameType> queue = new ConcurrentQueue<TServerDataFrameType>();
        
        private readonly Timer handshakeTimer;
        private readonly Timer dataReceiveTimer;
        private readonly Timer keepAlivePacketSenderTimer;

        private readonly TimeSpan handshakeTimeout;
        private readonly TimeSpan dataReceiveTimeout;

        #region Events

        public event EventHandler<FrameReceivedEventArgs<TClientDataFrameType>> FrameReceived;

        public event EventHandler<SessionStateChangedEventArgs> StateChanged;

        public event EventHandler<SessionClosedEventArgs> SessionClosed;

        #endregion

        #region Event Invocators

        protected virtual void OnSessionClosed(SessionClosedEventArgs e)
        {
            EventHandler<SessionClosedEventArgs> handler = this.SessionClosed;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnStateChanged(SessionStateChangedEventArgs e)
        {
            EventHandler<SessionStateChangedEventArgs> handler = this.StateChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnFrameReceived(FrameReceivedEventArgs<TClientDataFrameType> e)
        {
            EventHandler<FrameReceivedEventArgs<TClientDataFrameType>> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        #endregion

        #region Properties

        public int ClientId
        {
            get { return this.clientId; }
        }

        public int CurrentLocalSequenceNr
        {
            get { return this.currentLocalSequenceNr; }
        }

        public SessionState SessionState
        {
            get { return this.sessionState; }
        }

        #endregion

        #region Constructor(s)

        public PollingClientSession(int clientId, IProtocolRuntimeSpefication protocolruntimeSpecification, TimeSpan handshakeTimeout, TimeSpan dataReceiveTimeout)
        {
            this.clientId = clientId;
            this.protocolruntimeSpecification = protocolruntimeSpecification;

            this.maxIncomingSequenceNr = protocolruntimeSpecification.MaxClientSequenceValue;
            this.maxOutgoingSequenceNr = protocolruntimeSpecification.MaxServerSequenceValue;

            this.handshakeTimeout = handshakeTimeout;
            this.dataReceiveTimeout = dataReceiveTimeout;

            this.currentLocalSequenceNr = new Random((int) DateTime.Now.Ticks).Next(1, this.maxOutgoingSequenceNr);

            this.frameBuffer = new FrameBuffer<TClientDataFrameType>(maxIncomingSequenceNr);
            this.frameBuffer.FrameBlockReceived += this.FrameBufferOnFrameBlockReceived;

            this.handshakeTimer = new Timer(this.HandshakeTimeoutCallback);
            this.dataReceiveTimer = new Timer(this.DataReceiveTimeoutCallback);
            this.keepAlivePacketSenderTimer = new Timer(this.KeepAlivePackSenderTimerCallback);
        }


        #endregion

        private void KeepAlivePackSenderTimerCallback(object state)
        {
            if (this.queue.IsEmpty)
            {
                this.queue.Enqueue(this.SetSequenceNr(new TServerDataFrameType()));
            }
        }

        private void HandshakeTimeoutCallback(object state)
        {
            this.SetNewSessionState(SessionState.Timeout);
            this.CloseSession(CloseReason.HandshakeTimeout);
        }

        private void DataReceiveTimeoutCallback(object state)
        {
            this.SetNewSessionState(SessionState.Timeout);
            this.CloseSession(CloseReason.ReceiveTimeout);
        }

        private void CloseSession(CloseReason reason)
        {
            this.OnSessionClosed(new SessionClosedEventArgs
            {
                ClientId = this.clientId,
                Reason = reason
            });

            this.SetNewSessionState(SessionState.Closed);
        }

        private void FrameBufferOnFrameBlockReceived(object sender, FrameBlockReceivedEventArgs<TClientDataFrameType> frameBlockReceivedEventArgs)
        {
            foreach (var frame in frameBlockReceivedEventArgs.Data)
            {
                this.OnFrameReceived(new FrameReceivedEventArgs<TClientDataFrameType>()
                {
                    Frame = frame
                });
            }
        }

        public void Send(TServerDataFrameType serverFrame)
        {
            if (this.sessionState != SessionState.Connected)
            {
                throw new Exception("Illegal state to send anything. Current state: " + this.SessionState);
            }

            this.queue.Enqueue(serverFrame);
        }

        internal TServerDataFrameType HandleConnectionRequest()
        {
            var response = new TServerDataFrameType
            {
                Payload = BitConverter.GetBytes(this.ClientId)
            };

            return this.SetSequenceNr(response);
        }

        public void Start()
        {
            this.SetNewSessionState(SessionState.Handshaking);

            this.handshakeTimer.Change(this.handshakeTimeout, Timeout.InfiniteTimeSpan);
        }

        private void SetNewSessionState(SessionState state)
        {
            if (this.sessionState != state)
            {
                var oldState = this.sessionState;
                this.sessionState = state;

                this.OnStateChanged(new SessionStateChangedEventArgs()
                {
                    PreviousState = oldState,
                    State = this.sessionState
                });
            }
        }

        private TServerDataFrameType SetSequenceNr(TServerDataFrameType frame)
        {
            lock (this.sequenceNrLock)
            {
                frame.SequenceId = this.currentLocalSequenceNr;
                this.currentLocalSequenceNr = this.currentLocalSequenceNr < this.maxOutgoingSequenceNr ? this.currentLocalSequenceNr + 1 : 0;
            }

            return frame;
        }

        internal TServerDataFrameType HandleClientFrame(ClientFrame clientFrame)
        {
            if (this.sessionState == SessionState.Handshaking)
            {
                this.SetNewSessionState(SessionState.Connected);

                this.keepAlivePacketSenderTimer.Change(this.protocolruntimeSpecification.KeepAliveServerInterval, this.protocolruntimeSpecification.KeepAliveServerInterval);
            }

            this.dataReceiveTimer.Change(this.dataReceiveTimeout, this.dataReceiveTimeout);

            if (clientFrame is TClientDataFrameType)
            {
                var dataFrame = (TClientDataFrameType)clientFrame;
                this.frameBuffer.Add(dataFrame);
            }

            TServerDataFrameType toSend;

            if (this.queue.TryDequeue(out toSend))
            {
                return this.SetSequenceNr(toSend);
            }

            return null;
        }

        protected virtual void Dispose(bool cleanupBothManagedAndNativeResources)
        {
            this.dataReceiveTimer.Dispose();
            this.handshakeTimer.Dispose();
            this.keepAlivePacketSenderTimer.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}