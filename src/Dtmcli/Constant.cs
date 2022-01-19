namespace Dtmcli
{
    internal class Constant
    {
        /// <summary>
        /// status for global/branch trans status.
        /// </summary>
        internal static readonly string StatusPrepared = "prepared";

        /// <summary>
        /// status for global trans status.
        /// </summary>
        internal static readonly string StatusSubmitted = "submitted";

        /// <summary>
        /// status for global/branch trans status.
        /// </summary>
        internal static readonly string StatusSucceed = "succeed";

        /// <summary>
        /// status for global/branch trans status.
        /// </summary>
        internal static readonly string StatusFailed = "failed";

        /// <summary>
        /// status for global trans status.
        /// </summary>
        internal static readonly string StatusAborting = "aborting";

        /// <summary>
        ///  branch type for TCC
        /// </summary>
        internal static readonly string BranchTry = "try";

        /// <summary>
        /// branch type for TCC
        /// </summary>
        internal static readonly string BranchConfirm = "confirm";

        /// <summary>
        /// branch type for TCC
        /// </summary>
        internal static readonly string BranchCancel = "cancel";

        /// <summary>
        /// branch type for XA
        /// </summary>
        internal static readonly string BranchCommit = "commit";

        /// <summary>
        /// branch type for XA
        /// </summary>
        internal static readonly string BranchRollback = "rollback";

        internal static readonly string ErrFailure = "FAILRUE";

        internal static readonly int FailureStatusCode = 400;

        internal class Request
        {
            internal static readonly string CONTENT_TYPE = "application/json";

            internal static readonly string URLBASE_PREFIX = "/api/dtmsvr/";

            internal static readonly string GID = "gid";

            internal static readonly string TRANS_TYPE = "trans_type";

            internal static readonly string BRANCH_ID = "branch_id";

            internal static readonly string DATA = "data";

            internal static readonly string TRY = "try";

            internal static readonly string CONFIRM = "confirm";

            internal static readonly string CANCEL = "cancel";

            internal static readonly string OP = "op";

            internal static readonly string CODE = "code";

            internal static readonly string MESSAGE = "message";

            internal static readonly string DTM_RESULT = "dtm_result";

            internal static readonly string OPERATION_PREPARE = "prepare";

            internal static readonly string OPERATION_SUBMIT = "submit";

            internal static readonly string OPERATION_ABORT = "abort";

            internal static readonly string OPERATION_REGISTERBRANCH = "registerBranch";

            /// <summary>
            /// branch type for message, SAGA, XA
            /// </summary>
            internal static readonly string BRANCH_ACTION = "action";

            /// <summary>
            /// branch type for SAGA
            /// </summary>
            internal static readonly string BRANCH_COMPENSATE = "compensate";

            internal static readonly string TYPE_TCC = "tcc";

            internal static readonly string TYPE_SAGA = "saga";
        }

        internal class Barrier
        {
            internal static readonly string TABLE_NAME = "dtm_barrier.barrier";

            internal static readonly string DBTYPE_MYSQL = "mysql";

            internal static readonly string DBTYPE_POSTGRES = "postgres";

            internal static readonly string PG_CONSTRAINT = "uniq_barrier";
        }
    }
}
