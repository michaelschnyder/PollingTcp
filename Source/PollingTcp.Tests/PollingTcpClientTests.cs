using System;
using System.Collections.Generic;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Shared;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingTcpClientTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InitializeClient_WithNoLinkLayer_ShoulThrowAnException()
        {
            var client = new TestPollingTcpClient(null);
            client.Connect();
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_ShouldRaiseConnectingStateViaEvent()
        {
            var client = new TestPollingTcpClient(new DummyClientClientTransportLayer());

            var numberOfConnectingEventsRaised = 0;
            client.ConnectionStateChanged += (sender, args) => numberOfConnectingEventsRaised += args.State == ConnectionState.Connecting ?  + 1 : 0;

            client.Connect();

            Assert.AreEqual(1, numberOfConnectingEventsRaised);        
            Assert.AreEqual(ConnectionState.Connecting, client.ConnectionState);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ConnectingClient_WhenConnection_ShouldRaiseException()
        {
            var client = new TestPollingTcpClient(new DummyClientClientTransportLayer());

            client.Connect();
            client.Connect();
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_SendAnEmptyClientId()
        {
            var networkLayer = new DummyClientClientTransportLayer();

            var client = new TestPollingTcpClient(networkLayer);
            
            client.Connect();

            Assert.AreEqual(1, networkLayer.Captured.Count);
            
            var sentFrameBytes = networkLayer.Captured[0];
            var sentFrame = new GenericSerializer<ClientDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.AreEqual(0, sentFrame.ClientId);
            Assert.IsNull(sentFrame.Payload);
        }

        [TestMethod]
        public void ConnectingClient_ReceivesResponseWhileConnecting_ShouldSwitchToConnectedState()
        {
            var connectionRequestResponse = new ServerDataFrame()
            {
                SequenceId = 7,
                Payload = BitConverter.GetBytes(12345)
            };

            var networkLayer = new DummyClientClientTransportLayer();

            var client = new TestPollingTcpClient(networkLayer);

            client.Connect();

            byte[] data = new GenericSerializer<ServerDataFrame>().Serialize(connectionRequestResponse);

            networkLayer.Receive(data);
            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);
        }

        [TestMethod]
        public void ConnectingClient_ReceivesResponseWhileConnecting_ShouldUseClientIdInData()
        {
            var connectionRequestResponse = new ServerDataFrame()
            {
                SequenceId = 7,
                Payload = BitConverter.GetBytes(12345)
            };

            var networkLayer = new DummyClientClientTransportLayer();

            var client = new TestPollingTcpClient(networkLayer);

            client.Connect();

            networkLayer.Receive(new GenericSerializer<ServerDataFrame>().Serialize(connectionRequestResponse));
            client.Send(new ClientDataFrame());
        }
    }

    class DummyClientClientTransportLayer : IClientNetworkLinkLayer
    {
        private readonly List<byte[]> captured = new List<byte[]>();
        public int MaxWindowSize { get; set; }
        public void Send(byte[] data)
        {
            this.captured.Add(data);
        }

        public List<byte[]> Captured
        {
            get { return this.captured; }
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            EventHandler<DataReceivedEventArgs> handler = this.DataReceived;
            if (handler != null) handler(this, e);
        }

        public void Receive(byte[] data)
        {
            this.OnDataReceived(new DataReceivedEventArgs()
            {
                Bytes = data
            });
        }

        public void StartPolling()
        {
        }

        public void StopPolling()
        {
        }
    }

    class TestPollingTcpClient : PollingTcpClient<ClientDataFrame, ServerDataFrame>
    {
        public TestPollingTcpClient(IClientNetworkLinkLayer clientNetworkLinkLayer) : base(clientNetworkLinkLayer, new BinaryClientFrameEncoder(), new BinaryServerFrameEncoder(), 10)
        {

        }
    }

    class GenericSerializer<TDataType>
    {
        public byte[] Serialize(TDataType obj)
        {
            var bformatter = new BinaryFormatter();
            var mStream = new MemoryStream();

            bformatter.Serialize(mStream, obj);
            return mStream.ToArray();
        }

        public TDataType Deserialze(byte[] data)
        {
            var bformatter = new BinaryFormatter();
            var mStream = new MemoryStream(data);

            var value = bformatter.Deserialize(mStream);
            return (TDataType)value;
        }
    }

    class BinaryClientFrameEncoder : FrameEncoder<ClientDataFrame>
    {
        GenericSerializer<ClientDataFrame> serializer = new GenericSerializer<ClientDataFrame>(); 

        public override ClientDataFrame Decode(byte[] bytes)
        {
            return serializer.Deserialze(bytes);
        }

        public override byte[] Encode(ClientDataFrame clientFrame)
        {
            return serializer.Serialize(clientFrame);
        }
    }

    class BinaryServerFrameEncoder : FrameEncoder<ServerDataFrame>
    {
        GenericSerializer<ServerDataFrame> serializer = new GenericSerializer<ServerDataFrame>(); 

        public override ServerDataFrame Decode(byte[] bytes)
        {
            return serializer.Deserialze(bytes);
        }

        public override byte[] Encode(ServerDataFrame clientFrame)
        {
            return serializer.Serialize(clientFrame);
        }
    }


}