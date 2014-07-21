using System;
using PollingTcp.Client;
using PollingTcp.Shared;

namespace PollingTcp.Common
{
    public class TransportLinkLayer<TSendDataType, TReceiveDataType>  
        where TSendDataType : DataFrame 
        where TReceiveDataType : DataFrame
    {
        private readonly FrameEncoder<TSendDataType> encoder;
        private readonly FrameEncoder<TReceiveDataType> decoder;
        private readonly int maxSequenceValue;
        private readonly INetworkLinkLayer networkLayer;

        private readonly FrameBuffer<TReceiveDataType> incomingBuffer;
        private int localSequenceNr;

        public event EventHandler<FrameReceivedEventArgs<TReceiveDataType>> FrameReceived;

        protected virtual void OnFrameReceived(FrameReceivedEventArgs<TReceiveDataType> e)
        {
            EventHandler<FrameReceivedEventArgs<TReceiveDataType>> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        public TransportLinkLayer(INetworkLinkLayer networkLayer, FrameEncoder<TSendDataType> encoder, FrameEncoder<TReceiveDataType> decoder, int maxSequenceValue)
        {
            this.networkLayer = networkLayer;
            this.encoder = encoder;
            this.decoder = decoder;
            this.maxSequenceValue = maxSequenceValue;

            this.incomingBuffer = new FrameBuffer<TReceiveDataType>(maxSequenceValue);
            this.incomingBuffer.FrameBlockReceived += this.IncomingBufferOnFrameBlockReceived;

            this.networkLayer.DataReceived += this.NetworkLayerOnDataReceived;

            this.localSequenceNr = new Random((int) DateTime.UtcNow.Ticks).Next(1, maxSequenceValue);
        }

        private void NetworkLayerOnDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            var decodedFrame = this.decoder.Decode(dataReceivedEventArgs.Bytes);

            this.incomingBuffer.Add(decodedFrame);
        }

        private void IncomingBufferOnFrameBlockReceived(object sender, FrameBlockReceivedEventArgs<TReceiveDataType> frameBlockReceivedEventArgs)
        {
            foreach (var dataFrame in frameBlockReceivedEventArgs.Data)
            {
                this.OnFrameReceived(new FrameReceivedEventArgs<TReceiveDataType>()
                {
                    Frame = dataFrame
                });
            }
        }

        public void Send(TSendDataType sendFrame)
        {
            // Set the sequenceId

            sendFrame.SequenceId = this.localSequenceNr;
            this.localSequenceNr++;
            if (this.localSequenceNr > this.maxSequenceValue)
            {
                this.localSequenceNr = 0;
            }

            // Encode the packet
            var bytesToSend = this.encoder.Encode(sendFrame);

            this.networkLayer.Send(bytesToSend);
        }
    }
}