namespace PollingTcp.Shared
{
    public class DataFrame
    {
        public int SequenceId { get; set; }
    }

    public class ClientDataFrame : DataFrame
    {
        public int ClientId { get; set; }
    }
}