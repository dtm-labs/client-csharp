using System;

namespace Dtmcli
{
    public class DtmOptions
    {
        public string DtmUrl { get; set; }

        /// <summary>
        /// dtm server http request timeout in milliseconds, default 100,000 milliseconds(100s)
        /// </summary>
        public int DtmHttpTimeout { get; set; } = 100 * 1000;

        /// <summary>
        /// branch http request timeout in milliseconds, default 100,000 milliseconds(100s)
        /// </summary>
        public int BranchHttpTimeout { get; set; } = 100 * 1000;

        public string DBType { get; set; } = "mysql";

        public string BarrierTableName { get; set; } = "dtm_barrier.barrier";
    }
}