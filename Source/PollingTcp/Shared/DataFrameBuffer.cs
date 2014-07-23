namespace PollingTcp.Shared
{
    public class DataFrameBuffer : FrameBuffer<SequencedDataFrame>
    {
        public DataFrameBuffer(int maxSequenceValue) : base(maxSequenceValue)
        {
        }
    }
}