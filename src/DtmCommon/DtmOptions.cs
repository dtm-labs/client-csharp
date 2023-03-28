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
        /// barrier sql table type. default mysql
        /// </summary>
        public string SqlDbType { get; set; } = "mysql";

        /// <summary>
        /// barrier sql table name, default dtm_barrier.barrier
        /// </summary>
        public string BarrierSqlTableName { get; set; } = "dtm_barrier.barrier";

        /// <summary>
        /// barrier MongoDB Database name, default is dtm_barrier
        /// </summary>
        public string BarrierMongoDbName { get; set; } = "dtm_barrier";

        /// <summary>
        /// barrier MongoDB Collection name, default is barrier
        /// </summary>
        public string BarrierMongoColName { get; set; } = "barrier";

        /// <summary>
        /// dtm server request timeout in milliseconds, default 10,000 milliseconds(10s)
        /// </summary>
        public int DtmTimeout { get; set; } = 10 * 1000;

        /// <summary>
        /// branch request timeout in milliseconds, default 10,000 milliseconds(10s)
        /// </summary>
        public int BranchTimeout { get; set; } = 10 * 1000;

        public string HttpCallback { get; set; }

        public string GrpcCallback { get; set; }
    }
}