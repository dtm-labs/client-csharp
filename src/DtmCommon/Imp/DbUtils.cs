using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace DtmCommon
{
    public class DbUtils
    {
        private readonly DtmOptions _options;
        private readonly DbSpecialDelegate _specialDelegate;

        public DbUtils(IOptions<DtmOptions> optionsAccs, DbSpecialDelegate specialDelegate)
        {
            _options = optionsAccs.Value;
            _specialDelegate = specialDelegate;
        }

        public async Task<(int, Exception)> InsertBarrier(DbConnection db, string transType, string gid, string branchID, string op, string barrierID, string reason, DbTransaction tx = null)
        {
            if (db == null) return (-1, null);
            if (string.IsNullOrWhiteSpace(op)) return (0, null);

            try
            {
                var str = string.Concat(_options.BarrierTableName, "(trans_type, gid, branch_id, op, barrier_id, reason) values(@trans_type,@gid,@branch_id,@op,@barrier_id,@reason)");
                var sql = _specialDelegate.GetDbSpecial().GetInsertIgnoreTemplate(str, Constant.Barrier.PG_CONSTRAINT);

                sql = _specialDelegate.GetDbSpecial().GetPlaceHoldSQL(sql);

                var affected = await db.ExecuteAsync(
                    sql,
                    new { trans_type = transType, gid = gid, branch_id = branchID, op = op, barrier_id = barrierID, reason = reason },
                    transaction: tx);

                return (affected, null);
            }
            catch (Exception ex)
            {
                return (0, ex);
            }
        }
    }

}