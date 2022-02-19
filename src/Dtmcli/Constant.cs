namespace Dtmcli
{
    internal class Constant
    {
        internal static readonly string DtmClientHttpName = "dtmClient";
        internal static readonly string BranchClientHttpName = "branchClient";
       
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

            internal static readonly string DTM = "dtm";

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

            internal static readonly string URL_NewGid = "/api/dtmsvr/newGid";
        }       
    }
}
