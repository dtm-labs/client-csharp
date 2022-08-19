using System.Collections.Generic;

namespace DtmCommon
{
    public class Constant
    {
        public static readonly string TYPE_TCC = "tcc";
        public static readonly string TYPE_SAGA = "saga";
        public static readonly string TYPE_MSG = "msg";

        public static readonly string ResultFailure = "FAILURE";
        public static readonly string ResultSuccess = "SUCCESS";
        public static readonly string ResultOngoing = "ONGOING";

        /// <summary>
        /// error of DUPLICATED for only msg
        /// if QueryPrepared executed before call. then DoAndSubmit return this error
        /// </summary>
        public static readonly string ResultDuplicated = "DUPLICATED";

        internal class Op
        {
            internal static readonly string Submit = "Submit";
            internal static readonly string Prepare = "Prepare";
            internal static readonly string Abort = "Abort";
        }

        internal class Md
        {
            internal static readonly string Gid = "dtm-gid";
            internal static readonly string TransType = "dtm-trans_type";
            internal static readonly string BranchId = "dtm-branch_id";
            internal static readonly string Op = "dtm-op";
            internal static readonly string Dtm = "dtm-dtm";
        }

        public class Barrier
        {
            public static readonly string DBTYPE_MYSQL = "mysql";
            public static readonly string DBTYPE_POSTGRES = "postgres";
            public static readonly string DBTYPE_SQLSERVER = "sqlserver";
            public static readonly string PG_CONSTRAINT = "uniq_barrier";
            public static readonly string MSG_BARRIER_REASON = "rollback";
            public static readonly string MSG_BRANCHID = "00";
            public static readonly string MSG_BARRIER_ID = "01";

            public static readonly Dictionary<string, string> OpDict = new Dictionary<string, string>()
            {
                { "cancel", "try" },
                { "compensate", "action" },
            };
            public static readonly string REDIS_LUA_CheckAdjustAmount = @" -- RedisCheckAdjustAmount
local v = redis.call('GET', KEYS[1])
local e1 = redis.call('GET', KEYS[2])

if v == false or v + ARGV[1] < 0 then
	return 'FAILURE'
end

if e1 ~= false then
	return 'DUPLICATE'
end

redis.call('SET', KEYS[2], 'op', 'EX', ARGV[3])

if ARGV[2] ~= '' then
	local e2 = redis.call('GET', KEYS[3])
	if e2 == false then
		redis.call('SET', KEYS[3], 'rollback', 'EX', ARGV[3])
		return
	end
end
redis.call('INCRBY', KEYS[1], ARGV[1])
";
            public static readonly string REDIS_LUA_QueryPrepared = @"-- RedisQueryPrepared
local v = redis.call('GET', KEYS[1])
if v == false then
	redis.call('SET', KEYS[1], 'rollback', 'EX', ARGV[1])
	v = 'rollback'
end
if v == 'rollback' then
	return 'FAILURE'
end
";
        }
    }

}