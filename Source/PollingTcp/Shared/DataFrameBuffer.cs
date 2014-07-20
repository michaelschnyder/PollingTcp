namespace PollingTcp.Shared
{
    public class DataFrameBuffer : FrameBuffer<DataFrame>
    {
        public DataFrameBuffer(int maxSequenceValue) : base(maxSequenceValue)
        {
        }
    }
}