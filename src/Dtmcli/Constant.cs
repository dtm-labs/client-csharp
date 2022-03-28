namespace Dtmcli
{
    internal static class Constant
    {
        internal const string DtmClientHttpName = "dtmClient";
        internal const string BranchClientHttpName = "branchClient";
       
        internal static class Request
        {
            internal const string CONTENT_TYPE = "application/json";

            internal const string URLBASE_PREFIX = "/api/dtmsvr/";

            internal const string GID = "gid";

            internal const string TRANS_TYPE = "trans_type";

            internal const string BRANCH_ID = "branch_id";

            internal const string DATA = "data";

            internal const string TRY = "try";

            internal const string CONFIRM = "confirm";

            internal const string CANCEL = "cancel";

            internal const string OP = "op";

            internal const string DTM = "dtm";

            internal const string CODE = "code";

            internal const string MESSAGE = "message";

            internal const string DTM_RESULT = "dtm_result";

            internal const string OPERATION_PREPARE = "prepare";

            internal const string OPERATION_SUBMIT = "submit";

            internal const string OPERATION_ABORT = "abort";

            internal const string OPERATION_REGISTERBRANCH = "registerBranch";

            /// <summary>
            /// branch type for message, SAGA, XA
            /// </summary>
            internal const string BRANCH_ACTION = "action";

            /// <summary>
            /// branch type for SAGA
            /// </summary>
            internal const string BRANCH_COMPENSATE = "compensate";

            internal const string URL_NewGid = "/api/dtmsvr/newGid";
        }       
    }
}
