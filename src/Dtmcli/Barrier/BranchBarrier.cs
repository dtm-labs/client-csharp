using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class BranchBarrier
    {
        public BranchBarrier(string transType, string gid, string branchID, string op, ILogger logger = null)
        {
            this.TransType = transType;
            this.Gid = gid;
            this.BranchID = branchID;
            this.Op = op;
            this.Logger = logger;
        }

        internal ILogger Logger { get; private set; }

        public string TransType { get; set; }

        public string Gid { get; set; }

        public string BranchID { get; set; }

        public string Op { get; set; }

        public int BarrierID { get; set; }

        public async Task Call(DbConnection db, Func<Task> busiCall)
        {
            this.BarrierID = this.BarrierID + 1;
            var bid = this.BarrierID.ToString().PadLeft(2, '0');

            // check the connection state
            if(db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            var tx = db.BeginTransaction();

            try
            {
                var originType = BarrierStatic.TypeDict.TryGetValue(this.Op, out var ot) ? ot : string.Empty;
                
                var originAffected = await DbUtils.InsertBarrier(db, this.TransType, this.Gid, this.BranchID, originType, bid, this.Op, tx);
                var currentAffected = await DbUtils.InsertBarrier(db, this.TransType, this.Gid, this.BranchID, this.Op, bid, this.Op, tx);

                Logger?.LogDebug("originAffected: {originAffected} currentAffected: {currentAffected}", originAffected, currentAffected);

                var isNullCompensation = IsNullCompensation(this.Op, originAffected);
                var isDuplicateOrPend = IsDuplicateOrPend(currentAffected);

                if (isNullCompensation || isDuplicateOrPend)
                {
                    Logger?.LogInformation("Will not exec busiCall, isNullCompensation={isNullCompensation}, isDuplicateOrPend={isDuplicateOrPend}", isNullCompensation, isDuplicateOrPend);
                    tx.Commit();
                    return;
                }

                await busiCall.Invoke();
                tx.Commit();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Call exception");
                tx.Rollback();
            }
        }

        public async Task<string> QueryPrepared(DbConnection db)
        {
            bool isErr = false;
            try
            {
                var tmp = DbUtils.InsertBarrier(db, this.TransType, this.Gid, "00", "msg", "01", "rollback");
            }
            catch (Exception ex)
            {
                Logger?.LogInformation(ex, "QueryPrepared error");
                isErr = true;
            }

            var reason = string.Empty;

            if (!isErr)
            {
                var sql = string.Format("select reason from {0} where gid=@gid and branch_id=@branch_id and op=@op and barrier_id=@barrier_id", Constant.Barrier.TABLE_NAME);

                reason = await db.QueryFirstOrDefaultAsync(
                    sql,
                    new { gid = this.Gid, branch_id = this.BranchID, op = this.Op, barrier_id = this.BarrierID });
            }

            if (reason.Equals("rollback")) return "FAILURE";

            return string.Empty;
        }

        /// <summary>
        /// 空补偿
        /// </summary>
        /// <param name="op"></param>
        /// <param name="originAffected"></param>
        /// <returns></returns>
        private bool IsNullCompensation(string op, int originAffected)
            => (op.Equals(Constant.BranchCancel) || op.Equals(Constant.Request.BRANCH_COMPENSATE)) && originAffected > 0;

        /// <summary>
        /// 这个是重复请求或者悬挂
        /// </summary>
        /// <param name="currentAffected"></param>
        /// <returns></returns>
        private bool IsDuplicateOrPend(int currentAffected)
            => currentAffected == 0;

        public bool IsInValid()
        {
            return string.IsNullOrWhiteSpace(this.TransType)
                || string.IsNullOrWhiteSpace(this.Gid)
                || string.IsNullOrWhiteSpace(this.BranchID)
                || string.IsNullOrWhiteSpace(this.Op);
        }

        public override string ToString()
            => $"transInfo: {TransType} {Gid} {BranchID} {Op}";
    }

    internal class BarrierStatic
    {
        internal static readonly Dictionary<string, string> TypeDict = new Dictionary<string, string>()
        {
            { Constant.BranchCancel, Constant.BranchTry },
            { Constant.Request.BRANCH_COMPENSATE, Constant.Request.BRANCH_ACTION },
        };
    }
}
