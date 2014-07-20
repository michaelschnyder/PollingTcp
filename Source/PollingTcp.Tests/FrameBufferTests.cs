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
        public void SingleFrame_AddedToBuffer_ReturnImediately()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var frame = new DataFrame { SequenceId = 5 };

            var buffer = new DataFrameBuffer(maxSequenceValue);
            buffer.FrameReceived += (sender, args) => receivedFrameId = args.Data[0].SequenceId;

            buffer.Add(frame);

            Assert.AreEqual(frame.SequenceId, receivedFrameId);
        }

        [TestMethod]
        public void TwoFrames_AddedInRightOrder_ShouldNotBeBuffered()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var firstFrame = new DataFrame { SequenceId = 5 };
            var secondFrame = new DataFrame { SequenceId = 6 };

            var cache = new DataFrameBuffer(maxSequenceValue);
            cache.FrameReceived += (sender, args) => receivedFrameId = args.Data[0].SequenceId;

            cache.Add(firstFrame);
            Assert.AreEqual(firstFrame.SequenceId, receivedFrameId);

            cache.Add(secondFrame);
            Assert.AreEqual(secondFrame.SequenceId, receivedFrameId);
        }

        [TestMethod]
        public void TwoFrames_AddedInWrongOrder_ShouldBeBuffered()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var initialFrame = new DataFrame { SequenceId = 4 };
            var firstFrame = new DataFrame { SequenceId = 5 };
            var secondFrame = new DataFrame { SequenceId = 6 };

            var buffer = new DataFrameBuffer(maxSequenceValue);
            buffer.FrameReceived += (sender, args) => receivedFrameId = args.Data.Last().SequenceId;

            buffer.Add(initialFrame);

            buffer.Add(secondFrame);
            Assert.AreEqual(initialFrame.SequenceId, receivedFrameId);

            buffer.Add(firstFrame);
            Assert.AreEqual(secondFrame.SequenceId, receivedFrameId);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AFrameWithTooHighFrameId_WhenAddedToBuffer_ShouldThrowAnException()
        {
            var maxSequenceValue = 10;

            var cache = new DataFrameBuffer(maxSequenceValue);
            cache.Add(new DataFrame() { SequenceId = maxSequenceValue + 1});
        }

        [TestMethod]
        public void TwoFramesWithOverrun_AddedInRightOrder_ShouldNotBuffer()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var firstFrame = new DataFrame { SequenceId = 10 };
            var secondFrame = new DataFrame { SequenceId = 0 };

            var buffer = new DataFrameBuffer(maxSequenceValue);
            buffer.FrameReceived += (sender, args) => receivedFrameId = args.Data.Last().SequenceId;

            buffer.Add(firstFrame);
            Assert.AreEqual(firstFrame.SequenceId, receivedFrameId);

            buffer.Add(secondFrame);
            Assert.AreEqual(secondFrame.SequenceId, receivedFrameId);
        }

        [TestMethod]
        public void TwoFramesWithOverrun_AddedInWrongOrder_ShouldBuffer()
        {
            var maxSequenceValue = 10;
            var receivedFrameId = 0;

            var initialFrame = new DataFrame { SequenceId = 9 };
            var firstFrame = new DataFrame { SequenceId = 10 };
            var secondFrame = new DataFrame { SequenceId = 0 };

            var buffer = new DataFrameBuffer(maxSequenceValue);
            buffer.FrameReceived += (sender, args) => receivedFrameId = args.Data.Last().SequenceId;

            buffer.Add(initialFrame);

            buffer.Add(secondFrame);
            Assert.AreEqual(initialFrame.SequenceId, receivedFrameId);

            buffer.Add(firstFrame);
            Assert.AreEqual(secondFrame.SequenceId, receivedFrameId);
        }
    }
}
