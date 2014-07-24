using PollingTcp.Shared;

namespace PollingTcp.Tests.Helper
{
    public class DataFrameBuffer : FrameBuffer<TestDataFrame>
    {
        public DataFrameBuffer(int maxSequenceValue) : base(maxSequenceValue)
        {
        }
    }
}