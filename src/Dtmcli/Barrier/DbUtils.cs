using Dapper;
using Dtmcli.DtmImp;
using System.Data.Common;
using System.Threading.Tasks;

namespace Dtmcli
{
    public static class DbUtils
    {
        public static async Task<int> InsertBarrier(DbConnection db, string transType, string gid, string branchID, string op, string barrierID, string reason, DbTransaction tx = null)
        {
            if (db == null) return -1;
            if (string.IsNullOrWhiteSpace(op)) return 0;

            var str = string.Concat(Constant.Barrier.TABLE_NAME, "(trans_type, gid, branch_id, op, barrier_id, reason) values(@trans_type,@gid,@branch_id,@op,@barrier_id,@reason)");
            var sql = DbSpecialDelegate.Instance.GetDBSpecial().GetInsertIgnoreTemplate(str, Constant.Barrier.PG_CONSTRAINT);

            sql = DbSpecialDelegate.Instance.GetDBSpecial().GetPlaceHoldSQL(sql);

            var result = await db.ExecuteAsync(
                sql, 
                new { trans_type = transType, gid = gid, branch_id = branchID, op = op, barrier_id = barrierID, reason = reason },
                transaction: tx);

            return result;
        }
    }
}
