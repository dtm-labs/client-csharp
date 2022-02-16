namespace Dtmcli
{
    public class DtmOptions
    {
        public string DtmUrl { get; set; }

        public string DBType { get; set; } = "mysql";

        public string BarrierTableName { get; set; } = "dtm_barrier.barrier";
    }
}
