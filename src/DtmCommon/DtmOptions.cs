namespace DtmCommon
{
    public class DtmOptions
    {
        /// <summary>
        /// dtm server http address, work for Dtmcli
        /// </summary>
        public string DtmUrl { get; set; }

        /// <summary>
        /// dtm server grpc address, work for Dtmgrpc
        /// </summary>
        public string DtmGrpcUrl { get; set; }

        /// <summary>
        /// barrier table type. default mysql
        /// </summary>
        public string DBType { get; set; } = "mysql";

        /// <summary>
        /// barrier table name, default dtm_barrier.barrier
        /// </summary>
        public string BarrierTableName { get; set; } = "dtm_barrier.barrier";

        /// <summary>
        /// dtm server request timeout in milliseconds, default 10,000 milliseconds(10s)
        /// </summary>
        public int DtmTimeout { get; set; } = 10 * 1000;

        /// <summary>
        /// branch request timeout in milliseconds, default 10,000 milliseconds(10s)
        /// </summary>
        public int BranchTimeout { get; set; } = 10 * 1000;
    }
}