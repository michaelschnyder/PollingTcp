using System;
using System.Collections.Concurrent;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Server
{
    public class ClientSession<TClientDataFrameType, TServerDataFrameType>
        where TClientDataFrameType : ClientFrame, ISequencedDataFrame where TServerDataFrameType : ServerDataFrame, new()
    {
        private int clientId;
        private readonly int maxOutgoingSequenceNr;
        private readonly int maxIncomingSequenceNr;
        private int currentLocalSequenceNr;

        private object sequenceNrLock = new object();
        private FrameBuffer<TClientDataFrameType> frameBuffer;
        private ConcurrentQueue<TServerDataFrameType> queue = new ConcurrentQueue<TServerDataFrameType>();

        public event EventHandler<FrameReceivedEventArgs<TClientDataFrameType>> FrameReceived;

        protected virtual void OnFrameReceived(FrameReceivedEventArgs<TClientDataFrameType> e)
        {
            EventHandler<FrameReceivedEventArgs<TClientDataFrameType>> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

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
            this.currentLocalSequenceNr = new Random((int)DateTime.Now.Ticks).Next(1, this.maxOutgoingSequenceNr);

            this.frameBuffer = new FrameBuffer<TClientDataFrameType>(maxIncomingSequenceNr);
            this.frameBuffer.FrameBlockReceived += this.FrameBufferOnFrameBlockReceived;
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

        internal TServerDataFrameType CreateAcceptResponse()
        {
            var response = new TServerDataFrameType
            {
                Payload = BitConverter.GetBytes(this.ClientId)
            };

            return this.SetSequenceNr(response);
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
            var dataFrame = (TClientDataFrameType)clientFrame;

            if (dataFrame != null)
            {
                this.frameBuffer.Add(dataFrame);
            }
            else
            {
                var controlFrame = (ClientControlFrame)clientFrame;

                if (controlFrame != null)
                {

                }
            }

            TServerDataFrameType toSend = null;

            if (this.queue.TryDequeue(out toSend))
            {
                return this.SetSequenceNr(toSend);
            }

            return null;
        }
    }
}