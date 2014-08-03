﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Server
{
    public class PollingClientSession<TClientDataFrameType, TServerDataFrameType>
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
        

        public event EventHandler<FrameReceivedEventArgs<TClientDataFrameType>> FrameReceived;

        public event EventHandler<SessionStateChangedEventArgs> StateChanged;

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

        public PollingClientSession(int clientId, IProtocolRuntimeSpefication protocolruntimeSpecification, TimeSpan handshakeTimeout, TimeSpan dataReceiveTimeout)
        {
            this.clientId = clientId;
            this.protocolruntimeSpecification = protocolruntimeSpecification;
            
            this.maxIncomingSequenceNr = protocolruntimeSpecification.MaxClientSequenceValue;
            this.maxOutgoingSequenceNr = protocolruntimeSpecification.MaxServerSequenceValue;

            this.handshakeTimeout = handshakeTimeout;
            this.dataReceiveTimeout = dataReceiveTimeout;

            this.currentLocalSequenceNr = new Random((int)DateTime.Now.Ticks).Next(1, this.maxOutgoingSequenceNr);

            this.frameBuffer = new FrameBuffer<TClientDataFrameType>(maxIncomingSequenceNr);
            this.frameBuffer.FrameBlockReceived += this.FrameBufferOnFrameBlockReceived;

            this.handshakeTimer = new Timer(this.HandshakeTimeoutCallback);
            this.dataReceiveTimer = new Timer(this.DataReceiveTimeoutCallback);
            this.keepAlivePacketSenderTimer = new Timer(this.KeepAlivePackSenderTimerCallback);
        }

        private void KeepAlivePackSenderTimerCallback(object state)
        {
            if (this.queue.IsEmpty)
            {
                this.queue.Enqueue(this.SetSequenceNr(new TServerDataFrameType()));
            }
        }

        private void HandshakeTimeoutCallback(object state)
        {

        }

        private void DataReceiveTimeoutCallback(object state)
        {

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
            this.queue.Enqueue(serverFrame);
        }

        internal TServerDataFrameType Accept()
        {
            var response = new TServerDataFrameType
            {
                Payload = BitConverter.GetBytes(this.ClientId)
            };

            this.SetNewSessionState(SessionState.Handshaking);

            this.handshakeTimer.Change(this.handshakeTimeout, Timeout.InfiniteTimeSpan);

            return this.SetSequenceNr(response);
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
    }
}