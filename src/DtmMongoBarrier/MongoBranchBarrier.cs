using DtmCommon;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using MongoDB.Driver;
using System.Linq;
using System.Collections.Generic;

namespace DtmMongoBarrier
{
    public static class MongoBranchBarrier
    {
        public static async Task MongoCall(this BranchBarrier bb, IMongoClient mc, Func<IClientSessionHandle, Task> busiCall)
        {
            bb.BarrierID = bb.BarrierID + 1;
            var bid = bb.BarrierID.ToString().PadLeft(2, '0');

            var session = await mc.StartSessionAsync();

            session.StartTransaction();

            try
            {
                var originOp = Constant.Barrier.OpDict.TryGetValue(bb.Op, out var ot) ? ot : string.Empty;

                var (originAffected, oEx) = await MongoInsertBarrier(bb, session, bb.BranchID, originOp, bid, bb.Op);
                var (currentAffected, rEx) = await MongoInsertBarrier(bb, session, bb.BranchID, bb.Op, bid, bb.Op);

                bb?.Logger?.LogDebug("mongo originAffected: {originAffected} currentAffected: {currentAffected}", originAffected, currentAffected);

                if (bb.IsMsgRejected(rEx?.Message, bb.Op, currentAffected))
                    throw new DtmDuplicatedException();

                if (oEx != null || rEx != null)
                {
                    throw oEx ?? rEx;
                }

                var isNullCompensation = bb.IsNullCompensation(bb.Op, originAffected);
                var isDuplicateOrPend = bb.IsDuplicateOrPend(currentAffected);

                if (isNullCompensation || isDuplicateOrPend)
                {
                    bb?.Logger?.LogInformation("mongo Will not exec busiCall, isNullCompensation={isNullCompensation}, isDuplicateOrPend={isDuplicateOrPend}", isNullCompensation, isDuplicateOrPend);                    
                    await session.CommitTransactionAsync();
                    return;
                }

                await busiCall.Invoke(session);

                await session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                bb?.Logger?.LogError(ex, "Mongo Call error, gid={gid}, trans_type={trans_type}", bb.Gid, bb.TransType);

                await session.AbortTransactionAsync();

                throw;
            }
        }

        public static async Task<string> MongoQueryPrepared(this BranchBarrier bb, IMongoClient mc)
        {
            var session = await mc.StartSessionAsync();

            try
            {
                 await MongoInsertBarrier(
                     bb,
                     session,
                     Constant.Barrier.MSG_BRANCHID,
                     Constant.TYPE_MSG,
                     Constant.Barrier.MSG_BARRIER_ID,
                     Constant.Barrier.MSG_BARRIER_REASON);
            }
            catch (Exception ex)
            {
                bb?.Logger?.LogWarning(ex, "Mongo Insert Barrier error, gid={gid}", bb.Gid);
                return ex.Message;
            }

            var reason = string.Empty;
           
            try
            {
                var fs = bb.DtmOptions.BarrierTableName.Split('.');
                var barrier = session.Client.GetDatabase(fs[0]).GetCollection<DtmBarrierDocument>(fs[1]);

                var filter = BuildFilters(bb.Gid, Constant.Barrier.MSG_BRANCHID, Constant.TYPE_MSG, Constant.Barrier.MSG_BARRIER_ID);
                var cursor = await barrier.FindAsync<DtmBarrierDocument>(filter);
                var res = await cursor.ToListAsync();

                if (res != null && res.Any())
                {
                    reason = res.First().Reason;
                    if (reason.Equals(Constant.Barrier.MSG_BARRIER_REASON)) return Constant.ResultFailure;
                }
            }
            catch (Exception ex)
            {
                bb?.Logger?.LogWarning(ex, "Mongo Query Prepared error, gid={gid}", bb.Gid);
                return ex.Message;
            }

            return string.Empty;
        }

        private static async Task<(int, Exception)> MongoInsertBarrier(BranchBarrier bb, IClientSessionHandle session, string branchId, string op, string bid, string reason)
        {
            Exception err = null;
            if (session == null) return (-1, err);
            if (string.IsNullOrWhiteSpace(op)) return (0, err);

            var fs = bb.DtmOptions.BarrierTableName.Split('.');
            var barrier = session.Client.GetDatabase(fs[0]).GetCollection<DtmBarrierDocument>(fs[1]);

            List<DtmBarrierDocument> res = null;

            try
            {
                var filter = BuildFilters(bb.Gid, branchId, op, bid);
                var cursor = await barrier.FindAsync<DtmBarrierDocument>(filter);
                res = await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                err = ex;
                bb?.Logger?.LogDebug(ex, "Find document exception here, gid={gid}, branchId={branchId}, op={op}, bid={bid}", bb.Gid, branchId, op, bid);
            }

            if (res == null || res.Count <= 0)
            {
                try
                {
                    await barrier.InsertOneAsync(new DtmBarrierDocument
                    {
                        TransType = bb.TransType,
                        GId = bb.Gid,
                        BranchId = bb.BranchID,
                        Op = op,
                        BarrierId = bid,
                        Reason = reason
                    });
                }
                catch (Exception ex)
                {
                    err = ex;
                }

                return (1, err);
            }

            return (0, err);
        }

        private static FilterDefinition<DtmBarrierDocument> BuildFilters(string gid, string branchId, string op, string barrierId)
        { 
            return new FilterDefinitionBuilder<DtmBarrierDocument>().And(
                    Builders<DtmBarrierDocument>.Filter.Eq(x => x.GId, gid),
                    Builders<DtmBarrierDocument>.Filter.Eq(x => x.BranchId, branchId),
                    Builders<DtmBarrierDocument>.Filter.Eq(x => x.Op, op),
                    Builders<DtmBarrierDocument>.Filter.Eq(x => x.BarrierId, barrierId)
                    );
        }
    }
}