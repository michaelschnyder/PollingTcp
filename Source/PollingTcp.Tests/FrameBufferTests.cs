using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Shared;

namespace PollingTcp.Tests
{
    [TestClass]
    public class FrameCacheTests
    {
        [TestMethod]
        public void EmptyCache_WhenSingleFrameAdded_ShouldNotBeBuffered()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var frame = new DataFrame { FrameId = 5 };

            var cache = new FrameBuffer(maxSequenceValue);
            cache.FrameReceived += (sender, args) => receivedFrameId = args.Data[0].FrameId;

            cache.Add(frame);

            Assert.AreEqual(frame.FrameId, receivedFrameId);
        }

        [TestMethod]
        public void EmptyCache_RightOrderedFramesAdded_ShouldNotBeBuffered()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var firstFrame = new DataFrame { FrameId = 5 };
            var secondFrame = new DataFrame { FrameId = 6 };

            var cache = new FrameBuffer(maxSequenceValue);
            cache.FrameReceived += (sender, args) => receivedFrameId = args.Data[0].FrameId;

            cache.Add(firstFrame);
            Assert.AreEqual(firstFrame.FrameId, receivedFrameId);

            cache.Add(secondFrame);
            Assert.AreEqual(secondFrame.FrameId, receivedFrameId);
        }

        [TestMethod]
        public void EmptyCache_TwoWrongOrderedFramesAdded_ShouldBeBuffered()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var initialFrame = new DataFrame { FrameId = 4 };
            var firstFrame = new DataFrame { FrameId = 5 };
            var secondFrame = new DataFrame { FrameId = 6 };

            var cache = new FrameBuffer(maxSequenceValue);
            cache.FrameReceived += (sender, args) => receivedFrameId = args.Data.Last().FrameId;

            cache.Add(initialFrame);

            cache.Add(secondFrame);
            Assert.AreEqual(initialFrame.FrameId, receivedFrameId);

            cache.Add(firstFrame);
            Assert.AreEqual(secondFrame.FrameId, receivedFrameId);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void EmptyCache_AddingAFrameWithTooHighFrameId_AddShouldThrowAnException()
        {
            var maxSequenceValue = 10;

            var cache = new FrameBuffer(maxSequenceValue);
            cache.Add(new DataFrame() { FrameId = maxSequenceValue + 1});
        }

        [TestMethod]
        public void EmptyCache_OverrunWithOrderedFrames_ShouldNotBuffer()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var firstFrame = new DataFrame { FrameId = 10 };
            var secondFrame = new DataFrame { FrameId = 0 };

            var cache = new FrameBuffer(maxSequenceValue);
            cache.FrameReceived += (sender, args) => receivedFrameId = args.Data.Last().FrameId;

            cache.Add(firstFrame);
            Assert.AreEqual(firstFrame.FrameId, receivedFrameId);

            cache.Add(secondFrame);
            Assert.AreEqual(secondFrame.FrameId, receivedFrameId);
        }

        [TestMethod]
        public void EmptyCache_OverrunWithUnOrderedFrames_ShouldBuffer()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var initialFrame = new DataFrame { FrameId = 9 };
            var firstFrame = new DataFrame { FrameId = 10 };
            var secondFrame = new DataFrame { FrameId = 0 };

            var cache = new FrameBuffer(maxSequenceValue);
            cache.FrameReceived += (sender, args) => receivedFrameId = args.Data.Last().FrameId;

            cache.Add(initialFrame);

            cache.Add(secondFrame);
            Assert.AreEqual(initialFrame.FrameId, receivedFrameId);

            cache.Add(firstFrame);
            Assert.AreEqual(secondFrame.FrameId, receivedFrameId);
        }
    }
}
