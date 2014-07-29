using System;
using PollingTcp.Client;
using PollingTcp.Shared;

namespace PollingTcp.Common
{
    public class ClientTransportLayer<TSendControlFrameType, TSendDataFrameType, TReceiveDataType>
        where TSendControlFrameType : ClientControlFrame
        where TSendDataFrameType : ClientDataFrame
        where TReceiveDataType : ServerDataFrame
    {
        private readonly IClientFrameEncoder<TSendControlFrameType, TSendDataFrameType> encoder;
        private readonly FrameEncoder<TReceiveDataType> decoder;
        private readonly int maxSequenceValue;
        private readonly IClientNetworkLinkLayer networkLayer;

        private readonly FrameBuffer<TReceiveDataType> incomingBuffer;
        private int localSequenceNr;

        public event EventHandler<FrameReceivedEventArgs<TReceiveDataType>> FrameReceived;

        protected virtual void OnFrameReceived(FrameReceivedEventArgs<TReceiveDataType> e)
        {
            EventHandler<FrameReceivedEventArgs<TReceiveDataType>> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        public ClientTransportLayer(IClientNetworkLinkLayer networkLayer, IClientFrameEncoder<TSendControlFrameType, TSendDataFrameType> encoder, FrameEncoder<TReceiveDataType> decoder, int maxSequenceValue)
        {
            this.networkLayer = networkLayer;

            this.encoder = encoder;
            this.decoder = decoder;
            this.maxSequenceValue = maxSequenceValue;

            this.incomingBuffer = new FrameBuffer<TReceiveDataType>(maxSequenceValue);
            this.incomingBuffer.FrameBlockReceived += this.IncomingBufferOnFrameBlockReceived;

            this.localSequenceNr = new Random((int) DateTime.UtcNow.Ticks).Next(1, maxSequenceValue);

            this.networkLayer.DataReceived += this.NetworkLayerOnDataReceived;
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
                // Process via process method first
                //if (this.ProcessFrame != null)
                //{
                //    var response = this.ProcessFrame(dataFrame);

                //    if (response != null)
                //    {
                //        frameBlockReceivedEventArgs.ReturnValue = response;
                //    }
                //}

                this.OnFrameReceived(new FrameReceivedEventArgs<TReceiveDataType>()
                {
                    Frame = dataFrame
                });
            }
        }

        public void Send(TSendDataFrameType sendFrame)
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
    
        public void Send(TSendControlFrameType sendFrame)
        {
            // Encode the packet
            var bytesToSend = this.encoder.Encode(sendFrame);

            this.networkLayer.Send(bytesToSend);
        }
    }

}