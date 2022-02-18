using System;

namespace Dtmcli
{
    public class DtmOptions
    {
        public string DtmUrl { get; set; }

        /// <summary>
        /// dtm server http request timeout in seconds, default 100
        /// </summary>
        public int DtmHttpTimeout { get; set; } = 100;

        /// <summary>
        /// branch http request timeout in seconds, default 100
        /// </summary>
        public int BranchHttpTimeout { get; set; } = 100;

        public string DBType { get; set; } = "mysql";

        public string BarrierTableName { get; set; } = "dtm_barrier.barrier";
    }
}