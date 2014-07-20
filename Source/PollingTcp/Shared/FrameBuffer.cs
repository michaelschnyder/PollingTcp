using System;
using System.Collections.Generic;
using System.Linq;

namespace PollingTcp.Shared
{
    public class FrameBuffer
    {
        private readonly int maxSequenceValue;

        readonly List<int> missingFrameIds = new List<int>();

        private bool isUnused = true;
        private readonly DataFrame[] buffer;
        
        private int remoteSequenceNr;
        private int localSequenceNr;
        private const double AcceptanceWindowTolerance = 0.3;

        public event EventHandler<FrameReceivedEventArgs> FrameReceived;

        protected virtual void OnFrameReceived(FrameReceivedEventArgs e)
        {
            EventHandler<FrameReceivedEventArgs> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        public FrameBuffer(int maxSequenceValue)
        {
            this.maxSequenceValue = maxSequenceValue;

            this.buffer = new DataFrame[maxSequenceValue + 1];
        }

        public void Add(DataFrame frame)
        {
            if (frame.FrameId > this.maxSequenceValue)
            {
                throw new ArgumentOutOfRangeException("frame", frame.FrameId, "The value of the frameId should be lower or equal the max sequence value defined.");
            }

            this.buffer[frame.FrameId] = frame;

            if (this.isUnused)
            {
                // is this the first frame
                this.localSequenceNr = frame.FrameId;
                
                this.OnFrameReceived(new FrameReceivedEventArgs()
                {
                    Data = new[] {frame}
                });

                this.buffer[frame.FrameId] = null;
            }
            else if (frame.FrameId == this.localSequenceNr + 1 || (localSequenceNr == this.maxSequenceValue && frame.FrameId == 0))
            {
                // Calculate Acceptance Window values
                var windowRange = this.maxSequenceValue * AcceptanceWindowTolerance;
                var lowerRange = (int)(this.remoteSequenceNr - windowRange / 2);
                var higherRange = (int)(this.remoteSequenceNr + windowRange / 2) + 1;

                bool rangeCheckSucceded;

                if (lowerRange >= 0 && higherRange <= this.maxSequenceValue)
                {
                    rangeCheckSucceded = (frame.FrameId >= lowerRange && frame.FrameId <= higherRange);
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

                    rangeCheckSucceded = (frame.FrameId >= lowerRange && frame.FrameId <= this.maxSequenceValue) || (frame.FrameId >= 0 && frame.FrameId <= higherRange);
                }

                if (!rangeCheckSucceded)
                {
                    throw new ArgumentOutOfRangeException("frame", frame.FrameId, string.Format("The frameId is should be in the range from {0} to {1}", lowerRange, higherRange));
                }


                // this is an expected frame
                if (this.missingFrameIds.Contains(frame.FrameId))
                {
                    // find complete ranges starting with the current localSqeucneNr up to the currentRemoteSequenceNr
                    var foundBlock = new List<DataFrame>();
                    
                    for (int sequenceId = localSequenceNr + 1; sequenceId <= this.maxSequenceValue + frame.FrameId; sequenceId++)
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
                        this.localSequenceNr = foundBlock.Last().FrameId;

                        this.OnFrameReceived(new FrameReceivedEventArgs()
                        {
                            Data = foundBlock.ToArray()
                        });
                    }
                }
                else
                {
                    this.localSequenceNr = frame.FrameId;

                    this.OnFrameReceived(new FrameReceivedEventArgs()
                    {
                        Data = new[] { frame }
                    });
                }
            }
            else
            {
                // There is at least one frame missing, add the current frame to the cache and continue
                this.buffer[frame.FrameId] = frame;

                for (int frameId = this.localSequenceNr + 1; frameId <= this.maxSequenceValue + frame.FrameId; frameId++)
                {
                    var bufferId = frameId % this.buffer.Length;

                    if (this.buffer[bufferId] == null)
                    {
                        this.missingFrameIds.Add(frameId);
                    }
                }
            }

            this.remoteSequenceNr = frame.FrameId > this.remoteSequenceNr || frame.FrameId < this.maxSequenceValue * AcceptanceWindowTolerance ? frame.FrameId : this.remoteSequenceNr;

            this.isUnused = false;

        }
    }
}