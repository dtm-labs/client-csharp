using Dapper;
using DtmCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using static Dtmcli.Constant;

namespace Dtmcli
{
    public sealed class XaLocalTransaction
    {
        private readonly DbSpecialDelegate _dbSpecia;
        private readonly IDtmClient _dtmClient;
        private readonly ILogger _logger;
        private readonly DbUtils _utils;

        public XaLocalTransaction(IDtmClient dtmClient, DbSpecialDelegate dbSpecia, DbUtils utils, ILoggerFactory factory)
        {
            this._dtmClient = dtmClient;
            this._dbSpecia = dbSpecia;
            this._utils = utils;
            this._logger = factory.CreateLogger<XaGlobalTransaction>();
        }

#if NET5_0_OR_GREATER
        public async Task ExcecuteAsync(Microsoft.AspNetCore.Http.IQueryCollection quersy, DbConnection conn, string dbType, Func<DbConnection, Xa, Task> xaFunc, CancellationToken token = default)
        {
            Xa xa = Xa.FromQuery(this._dtmClient, quersy);
            await this.InternalExcecuteAsync(xa, conn, dbType, xaFunc, token);
        }
#else
        public async Task ExcecuteAsync(IDictionary<string, string> values, DbConnection conn, string dbType, Func<DbConnection, Xa, Task> xaFunc, CancellationToken token = default)
        {
            var xa = Xa.FromQuery(this._dtmClient, values);
            await this.InternalExcecuteAsync(xa, conn, dbType, xaFunc, token);
        }
#endif

        private async Task InternalExcecuteAsync(Xa xa, DbConnection conn, string dbType, Func<DbConnection, Xa, Task> xaFunc, CancellationToken token = default)
        {
            var dbSpecial = this._dbSpecia.GetDbSpecialByName(dbType);
            if (ConnectionState.Open != conn.State)
            {
                await conn.OpenAsync();
            }

            if (DtmCommon.Constant.OpCommit == xa.Op || DtmCommon.Constant.OpRollback == xa.Op)
            {
                await XaHandlePhase2(xa, conn, dbSpecial);
            }
            else
            {
                await HandleLocalTrans(xa, conn, dbSpecial, xaFunc, token);
            }
        }

        private async Task HandleLocalTrans(Xa xa, DbConnection conn, IDbSpecial dbSpecial, Func<DbConnection, Xa, Task> outsideAction, CancellationToken token)
        {
            var xaBranchID = $"{xa.Gid}-{xa.BranchIDGen.BranchID}";
            await conn.ExecuteAsync(dbSpecial.GetXaSQL("start", xaBranchID));
            var (_, ex) = await this._utils.InsertBarrier(conn, xa.TransType, xa.Gid, xa.BranchIDGen.BranchID, DtmCommon.Constant.OpAction, xa.BranchIDGen.BranchID, DtmCommon.Constant.OpAction);
            if (ex != null || conn.State != ConnectionState.Open)
                throw new DtmOngingException(ex?.Message);

            await outsideAction(conn, xa);
            await _dtmClient.TransRegisterBranch(xa, this.BuildRegisterItems(xa), Constant.Request.OPERATION_REGISTERBRANCH, token);
            await conn.ExecuteAsync(dbSpecial.GetXaSQL("end", xaBranchID));
            await conn.ExecuteAsync(dbSpecial.GetXaSQL("prepare", xaBranchID));
        }

        private async Task XaHandlePhase2(Xa xa, DbConnection conn, IDbSpecial dbSpecia)
        {
            try
            {
                var xaBranchID = $"{xa.Gid}-{xa.BranchIDGen.BranchID}";
                var xaCommon = xa.Op == DtmCommon.Constant.OpCommit ? "commit" : "rollback";
                await conn.ExecuteAsync(dbSpecia.GetXaSQL(xaCommon, xaBranchID));
            }
            catch (Exception ex) when (ex.Message.Contains("XAER_NOTA")) { }
            catch (Exception ex) when (ex.Message.Contains("does not exist")) { }
            catch
            {
                throw;
            }

            if (DtmCommon.Constant.OpRollback == xa.Op)
            {
                var (_, ex) = await this._utils.InsertBarrier(conn, "xa", xa.Gid, xa.BranchIDGen.BranchID, DtmCommon.Constant.OpAction, xa.BranchIDGen.BranchID, xa.Op);
                if (ex != null || conn.State != ConnectionState.Open)
                    throw new DtmOngingException(ex?.Message);
            }
        }

        private Dictionary<string, string> BuildRegisterItems(Xa xa)
        {
            return new Dictionary<string, string> {
                { "branch_id", xa.BranchIDGen.BranchID},
                { "url", xa.Phase2Url }
            };
        }
    }
}
