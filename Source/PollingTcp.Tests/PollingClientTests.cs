﻿using System;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PollingTcp.Client;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingClientTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InitializeClient_WithNoLinkLayer_ShoulThrowAnException()
        {
            var client = new TestPollingClient(null); 
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_ShouldRaiseConnectingStateViaEvent()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());

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
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());

            client.Connect();
            client.Connect();
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_SendAnEmptyClientId()
        {
            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);
            
            client.Connect();

            Assert.AreEqual(1, networkLayer.SentBytes.Count);
            
            var sentFrameBytes = networkLayer.SentBytes[0];
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

            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);

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

            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);

            client.Connect();

            networkLayer.Receive(new GenericSerializer<ServerDataFrame>().Serialize(connectionRequestResponse));
            client.Send(new ClientDataFrame());
        }
    }

    class TestPollingClient : PollingClient<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer) : base(clientNetworkLinkLayer, new BinaryClientControlFrameEncoder(), new BinaryClientDataFrameEncoder(), new BinaryServerFrameEncoder(), 10)
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
}