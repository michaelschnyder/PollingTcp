using PollingTcp.Frame;

namespace PollingTcp.Tests.Helper
{
    public class TestDataFrame : DataFrame, ISequencedDataFrame
    {
        public int SequenceId { get; set; }
    }
}