using System;
using System.Collections.Generic;
using System.Linq;

namespace PollingTcp.Shared
{
    public class FrameBuffer<TDataFrameType> where TDataFrameType : SequencedDataFrame
    {
        private readonly int maxSequenceValue;

        readonly List<int> missingFrameIds = new List<int>();

        private bool isUnused = true;
        private readonly TDataFrameType[] buffer;
        
        private int remoteSequenceNr;
        private int localSequenceNr;
        private const double AcceptanceWindowTolerance = 0.3;

        public event EventHandler<FrameBlockReceivedEventArgs<TDataFrameType>> FrameBlockReceived;

        protected virtual void OnFrameReceived(FrameBlockReceivedEventArgs<TDataFrameType> e)
        {
            EventHandler<FrameBlockReceivedEventArgs<TDataFrameType>> handler = this.FrameBlockReceived;
            if (handler != null) handler(this, e);
        }

        public FrameBuffer(int maxSequenceValue)
        {
            this.maxSequenceValue = maxSequenceValue;

            this.buffer = new TDataFrameType[maxSequenceValue + 1];
        }

        public object Add(TDataFrameType frame)
        {
            object retVal = null;

            if (frame.SequenceId > this.maxSequenceValue)
            {
                throw new ArgumentOutOfRangeException("frame", frame.SequenceId, "The value of the frameId should be lower or equal the max sequence value defined.");
            }

            this.buffer[frame.SequenceId] = frame;

            if (this.isUnused)
            {
                // is this the first frame
                this.localSequenceNr = frame.SequenceId;

                retVal = this.RaiseEventHandler(new [] { frame });

                this.buffer[frame.SequenceId] = null;
            }
            else if (frame.SequenceId == this.localSequenceNr + 1 || (localSequenceNr == this.maxSequenceValue && frame.SequenceId == 0))
            {
                // Calculate Acceptance Window values
                var windowRange = this.maxSequenceValue * AcceptanceWindowTolerance;
                var lowerRange = (int)(this.remoteSequenceNr - windowRange / 2);
                var higherRange = (int)(this.remoteSequenceNr + windowRange / 2) + 1;

                bool rangeCheckSucceded;

                if (lowerRange >= 0 && higherRange <= this.maxSequenceValue)
                {
                    rangeCheckSucceded = (frame.SequenceId >= lowerRange && frame.SequenceId <= higherRange);
                }
                else
                {
                    // This is a overrun situation
                    if (higherRange > this.maxSequenceValue)
                    {
                        higherRange = higherRange % this.buffer.Length;
                    }
                    else if (lowerRange < 0)
                    {
                        lowerRange = this.maxSequenceValue - lowerRange - 1;
                    }

                    rangeCheckSucceded = (frame.SequenceId >= lowerRange && frame.SequenceId <= this.maxSequenceValue) || (frame.SequenceId >= 0 && frame.SequenceId <= higherRange);
                }

                if (!rangeCheckSucceded)
                {
                    throw new ArgumentOutOfRangeException("frame", frame.SequenceId, string.Format("The frameId is should be in the range from {0} to {1}", lowerRange, higherRange));
                }

                // this is an expected frame
                if (this.missingFrameIds.Contains(frame.SequenceId))
                {
                    // find complete ranges starting with the current localSqeucneNr up to the currentRemoteSequenceNr
                    var foundBlock = new List<TDataFrameType>();
                    
                    for (int sequenceId = localSequenceNr + 1; sequenceId <= this.maxSequenceValue + frame.SequenceId; sequenceId++)
                    {
                        var bufferId = sequenceId % this.buffer.Length;
                        if (this.buffer[bufferId] != null)
                        {
                            foundBlock.Add(this.buffer[bufferId]);
                            this.buffer[bufferId] = null;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (foundBlock.Any())
                    {
                        this.localSequenceNr = foundBlock.Last().SequenceId;
                        this.RaiseEventHandler(foundBlock.ToArray());
                    }
                }
                else
                {
                    this.localSequenceNr = frame.SequenceId;

                    this.RaiseEventHandler(new [] { frame });
                }
            }
            else
            {
                // There is at least one frame missing, add the current frame to the cache and continue
                this.buffer[frame.SequenceId] = frame;

                for (int frameId = this.localSequenceNr + 1; frameId <= this.maxSequenceValue + frame.SequenceId; frameId++)
                {
                    var bufferId = frameId % this.buffer.Length;

                    if (this.buffer[bufferId] == null)
                    {
                        this.missingFrameIds.Add(frameId);
                    }
                }
            }

            this.remoteSequenceNr = frame.SequenceId > this.remoteSequenceNr || frame.SequenceId < this.maxSequenceValue * AcceptanceWindowTolerance ? frame.SequenceId : this.remoteSequenceNr;

            this.isUnused = false;

            return retVal;
        }

        private object RaiseEventHandler(TDataFrameType[] frameBlock)
        {
            object retVal;
            var args = new FrameBlockReceivedEventArgs<TDataFrameType>()
            {
                Data = frameBlock
            };

            this.OnFrameReceived(args);
            retVal = args.ReturnValue;
            return retVal;
        }
    }
}