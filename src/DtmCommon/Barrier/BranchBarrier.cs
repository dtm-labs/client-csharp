using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace DtmCommon
{
    public class BranchBarrier
    {
        private static readonly string Cancel = "cancel";
        private static readonly string Compensate = "compensate";
        private static readonly string QueryPreparedSqlFormat = "select reason from {0} where gid=@gid and branch_id=@branch_id and op=@op and barrier_id=@barrier_id";

        public BranchBarrier(string transType, string gid, string branchID, string op, DtmOptions options, DbUtils utils, ILogger logger = null)
        {
            this.TransType = transType;
            this.Gid = gid;
            this.BranchID = branchID;
            this.Op = op;
            this.Logger = logger;
            this.DtmOptions = options;
            this.DbUtils = utils;
        }

        internal DbUtils DbUtils { get; private set; }

        internal DtmOptions DtmOptions { get; private set; }

        public ILogger Logger { get; private set; }

        public string TransType { get; set; }

        public string Gid { get; set; }

        public string BranchID { get; set; }

        public string Op { get; set; }

        public int BarrierID { get; set; }

        public async Task Call(DbConnection db, Func<DbTransaction, Task> busiCall)
        {
            this.BarrierID = this.BarrierID + 1;
            var bid = this.BarrierID.ToString().PadLeft(2, '0');

            // check the connection state
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

#if NETSTANDARD2_0
            var tx = db.BeginTransaction();
#else
            var tx = await db.BeginTransactionAsync();
#endif

            try
            {
                var originOp = Constant.Barrier.OpDict.TryGetValue(this.Op, out var ot) ? ot : string.Empty;

                var (originAffected, oEx) = await DbUtils.InsertBarrier(db, this.TransType, this.Gid, this.BranchID, originOp, bid, this.Op, tx);
                if (oEx != null || db.State != ConnectionState.Open)
                {
                    throw new DtmOngingException(oEx?.Message);
                }
                var (currentAffected, rEx) = await DbUtils.InsertBarrier(db, this.TransType, this.Gid, this.BranchID, this.Op, bid, this.Op, tx);
                if (rEx != null || db.State != ConnectionState.Open)
                {
                    throw new DtmOngingException(rEx?.Message);
                }
                Logger?.LogDebug("originAffected: {originAffected} currentAffected: {currentAffected}", originAffected, currentAffected);

                if (IsMsgRejected(rEx?.Message, this.Op, currentAffected))
                    throw new DtmDuplicatedException();

                if (oEx != null || rEx != null)
                {
                    throw oEx ?? rEx;
                }

                var isNullCompensation = IsNullCompensation(this.Op, originAffected);
                var isDuplicateOrPend = IsDuplicateOrPend(currentAffected);

                if (isNullCompensation || isDuplicateOrPend)
                {
                    Logger?.LogInformation("Will not exec busiCall, isNullCompensation={isNullCompensation}, isDuplicateOrPend={isDuplicateOrPend}", isNullCompensation, isDuplicateOrPend);
#if NETSTANDARD2_0
                    tx.Commit();
#else
                    await tx.CommitAsync();
#endif
                    return;
                }

                await busiCall.Invoke(tx);
#if NETSTANDARD2_0
                tx.Commit();
#else
                await tx.CommitAsync();
#endif
            }
            catch (DtmException e)
            {
                Logger?.LogInformation($"dtm known {e.Message}, gid={this.Gid}, trans_type={this.TransType}");
                throw;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Call error, gid={gid}, trans_type={trans_type}", this.Gid, this.TransType);
                
#if NETSTANDARD2_0
                tx.Rollback();
#else
                await tx.RollbackAsync();
#endif

                throw;
            }
        }
        
         public async Task Call(DbConnection db, Func<Task> busiCall, TransactionScopeOption transactionScope = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            this.BarrierID = this.BarrierID + 1;
            var bid = this.BarrierID.ToString().PadLeft(2, '0');

            // check the connection state
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            using (var scope = new TransactionScope(transactionScope, new TransactionOptions { IsolationLevel = isolationLevel }))
            {
                try
                {
                    var originOp = Constant.Barrier.OpDict.TryGetValue(this.Op, out var ot) ? ot : string.Empty;
                    var (originAffected, oEx) = await DbUtils.InsertBarrier(db, this.TransType, this.Gid, this.BranchID, originOp, bid, this.Op);
                    if (oEx != null || db.State != ConnectionState.Open)
                    {
                        throw new DtmOngingException(oEx?.Message);
                    }
                    var (currentAffected, rEx) = await DbUtils.InsertBarrier(db, this.TransType, this.Gid, this.BranchID, this.Op, bid, this.Op);
                    if (rEx != null || db.State != ConnectionState.Open)
                    {
                        throw new DtmOngingException(rEx?.Message);
                    }
                    Logger?.LogDebug("originAffected: {originAffected} currentAffected: {currentAffected}", originAffected, currentAffected);

                    if (IsMsgRejected(rEx?.Message, this.Op, currentAffected))
                        throw new DtmDuplicatedException();

                    if (oEx != null || rEx != null)
                    {
                        throw oEx ?? rEx;
                    }

                    var isNullCompensation = IsNullCompensation(this.Op, originAffected);
                    var isDuplicateOrPend = IsDuplicateOrPend(currentAffected);

                    if (isNullCompensation || isDuplicateOrPend)
                    {
                        Logger?.LogInformation("Will not exec busiCall, isNullCompensation={isNullCompensation}, isDuplicateOrPend={isDuplicateOrPend}", isNullCompensation, isDuplicateOrPend);
                        return;
                    }
                    await busiCall.Invoke();
                    scope.Complete();
                }
                catch (DtmException e)
                {
                    Logger?.LogInformation($"dtm known {e.Message}, gid={this.Gid}, trans_type={this.TransType}");
                    throw;
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Call error, gid={gid}, trans_type={trans_type}", this.Gid, this.TransType);
                    throw;
                }
            }
           
        }

        public async Task<string> QueryPrepared(DbConnection db)
        {
            try
            {
                var tmp = await DbUtils.InsertBarrier(
                    db,
                    this.TransType,
                    this.Gid,
                    Constant.Barrier.MSG_BRANCHID,
                    Constant.TYPE_MSG,
                    Constant.Barrier.MSG_BARRIER_ID,
                    Constant.Barrier.MSG_BARRIER_REASON);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Insert Barrier error, gid={gid}", this.Gid);
                return ex.Message;
            }

            var reason = string.Empty;

            var sql = string.Format(QueryPreparedSqlFormat, DtmOptions.BarrierTableName);

            try
            {
                reason = await db.QueryFirstOrDefaultAsync<string>(
                           sql,
                           new { gid = this.Gid, branch_id = Constant.Barrier.MSG_BRANCHID, op = Constant.TYPE_MSG, barrier_id = Constant.Barrier.MSG_BARRIER_ID });

                if (reason.Equals(Constant.Barrier.MSG_BARRIER_REASON)) return Constant.ResultFailure;
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Query Prepared error, gid={gid}", this.Gid);
                return ex.Message;
            }

            return string.Empty;
        }

        /// <summary>
        /// 空补偿
        /// </summary>
        /// <param name="op"></param>
        /// <param name="originAffected"></param>
        /// <returns></returns>
        internal bool IsNullCompensation(string op, int originAffected)
            => (op.Equals(Cancel) || op.Equals(Compensate)) && originAffected > 0;

        /// <summary>
        /// 这个是重复请求或者悬挂
        /// </summary>
        /// <param name="currentAffected"></param>
        /// <returns></returns>
        internal bool IsDuplicateOrPend(int currentAffected)
            => currentAffected == 0;

        /// <summary>
        /// for msg's DoAndSubmit, repeated insert should be rejected.
        /// </summary>
        /// <param name="err">Barrier insert error</param>
        /// <param name="op">op</param>
        /// <param name="currentAffected">currentAffected</param>
        /// <returns></returns>
        internal bool IsMsgRejected(string err, string op, int currentAffected)
            => string.IsNullOrWhiteSpace(err) && op.Equals(Constant.TYPE_MSG) && currentAffected == 0;

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
}