using System;
using System.Collections.Generic;
using System.Linq;
using PollingTcp.Shared.TunnelMeOut.Common.Shared;

namespace PollingTcp.Shared
{
    public class FrameBuffer
    {
        private readonly int maxSequenceValue;

        readonly List<int> missingFrameIds = new List<int>();
        List<DataFrame> cache = new List<DataFrame>();

        private int maxReceivedFrameId;
        private int localProcessedFrameId;
        private bool isUnused = true;

        public event EventHandler<FrameReceivedEventArgs> FrameReceived;

        public int LocalProcessedFrameId
        {
            get { return this.localProcessedFrameId; }
        }

        protected virtual void OnFrameReceived(FrameReceivedEventArgs e)
        {
            EventHandler<FrameReceivedEventArgs> handler = this.FrameReceived;
            if (handler != null) handler(this, e);
        }

        public FrameBuffer(int maxSequenceValue)
        {
            this.maxSequenceValue = maxSequenceValue;
        }

        public void Add(DataFrame frame)
        {
            if (this.cache.Count == 0 && (this.isUnused || frame.FrameId == this.LocalProcessedFrameId + 1 || (frame.FrameId == 0 && this.LocalProcessedFrameId == this.maxSequenceValue)))
            {
                // Everthing is ok
                this.localProcessedFrameId = frame.FrameId;

                this.OnFrameReceived(new FrameReceivedEventArgs()
                {
                    Data = new[] { frame }
                });
            }
            else
            {
                var isOverrun = this.cache.Count > 0 && this.cache.Last().FrameId == this.maxSequenceValue + 1;
                
                this.cache.Add(frame);

                if (isOverrun)
                {
                    this.cache = this.cache.OrderByWithGap(clientFrame => clientFrame.FrameId).ToList();
                }
                else
                {
                    this.cache = this.cache.OrderBy(f => f.FrameId).ToList();
                }

                if (!this.missingFrameIds.Contains(frame.FrameId))
                {
                    this.RecordMissingFrames(frame);
                }
                else
                {
                    var firstFrameId = this.cache[0].FrameId;

                    if (firstFrameId == this.LocalProcessedFrameId + 1)
                    {
                        var lastFrameId = this.cache.Last().FrameId > this.LocalProcessedFrameId ? this.cache.Last().FrameId : this.maxSequenceValue + this.cache.Last().FrameId;
                        var i = 0;

                        var newFrames = new List<DataFrame>();

                        for (var cachedFrameId = firstFrameId; cachedFrameId <= lastFrameId; cachedFrameId++)
                        {
                            var expectedFrameId = cachedFrameId % this.maxSequenceValue;

                            if (this.cache[i].FrameId == expectedFrameId)
                            {
                                newFrames.Add(this.cache[i]);
                                i++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (newFrames.Count > 0)
                        {
                            // Increase the current SequenceId
                            this.localProcessedFrameId = newFrames.Last().FrameId;

                            foreach (var clientFrame in newFrames)
                            {
                                this.cache.Remove(clientFrame);
                            }

                            this.OnFrameReceived(new FrameReceivedEventArgs()
                            {
                                Data = newFrames.ToArray()
                            });
                        }
                    }
                }
            }

            if (frame.FrameId > this.maxReceivedFrameId)
            {
                this.maxReceivedFrameId = frame.FrameId;
            }
            else if (frame.FrameId == 0)
            {
                this.maxReceivedFrameId = 0;
            }

            this.isUnused = false;
        }

        private void RecordMissingFrames(DataFrame frame)
        {
            var fillUpTo = frame.FrameId > this.LocalProcessedFrameId ? frame.FrameId : this.maxSequenceValue + frame.FrameId;

            // Add missing frames 
            for (int missingFrameId = this.LocalProcessedFrameId + 1; missingFrameId < fillUpTo; missingFrameId++)
            {
                var frameId = missingFrameId%this.maxSequenceValue;

                if (!this.missingFrameIds.Contains(frameId))
                {
                    this.missingFrameIds.Add(frameId);
                }
            }
        }

        internal byte[] GetData()
        {
            return null;
        }
    }

    namespace TunnelMeOut.Common.Shared
    {
        public class FrameReceivedEventArgs : EventArgs
        {
            public int ClientId { get; set; }
            public DataFrame[] Data { get; set; }
        }
    }
}