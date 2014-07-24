using PollingTcp.Shared;

namespace PollingTcp.Tests
{
    public class TestDataFrame : DataFrame, ISequencedDataFrame
    {
        public int SequenceId { get; set; }
    }
}