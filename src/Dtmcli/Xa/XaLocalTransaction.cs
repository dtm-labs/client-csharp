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

        public async Task Excecute(IDictionary<string, string> values, DbConnection conn, string dbType, Func<DbConnection, Xa, Task> xaFunc, CancellationToken token = default)
        {
            var xa = this.XaFromQuery(values);
            var dbSpecial = this._dbSpecia.GetDbSpecialByName(dbType);
            if (ConnectionState.Open != conn.State)
            {
                conn.Open();
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

        private async Task HandleLocalTrans(Xa xa, DbConnection conn, IDbSpecial dbSpecial, Func<DbConnection, Xa,Task> outsideAction, CancellationToken token)
        {
            var xaBranchID = $"{xa.Gid}-{xa.BranchIDGen.BranchID}";
            await conn.ExecuteAsync(dbSpecial.GetXaSQL("start", xaBranchID));
            await this._utils.InsertBarrier(conn, xa.TransType, xa.Gid, xa.BranchIDGen.BranchID, DtmCommon.Constant.OpAction, xa.BranchIDGen.BranchID, DtmCommon.Constant.OpAction);
            await outsideAction(conn, xa);
            await _dtmClient.TransRegisterBranch(xa, this.BuildRegisterItems(xa), Constant.Request.OPERATION_REGISTERBRANCH, token);
            await conn.ExecuteAsync(dbSpecial.GetXaSQL("end", xaBranchID));
            await conn.ExecuteAsync(dbSpecial.GetXaSQL("prepare", xaBranchID));
        }

        private async Task XaHandlePhase2(Xa xa, DbConnection conn, IDbSpecial dbSpecia)
        {
            var xaBranchID = $"{xa.Gid}-{xa.BranchIDGen.BranchID}";
            var xaCommon = xa.Op == DtmCommon.Constant.OpCommit ? "commit" : "rollback";
            await conn.ExecuteAsync(dbSpecia.GetXaSQL(xaCommon, xaBranchID));
            if (DtmCommon.Constant.OpRollback == xa.Op)
            {
                await this._utils.InsertBarrier(conn, "xa", xa.Gid, xa.BranchIDGen.BranchID, DtmCommon.Constant.OpAction, xa.BranchIDGen.BranchID, xa.Op);
            }
        }

        private Xa XaFromQuery(IDictionary<string, string> values)
        {
            Xa xa = new(this._dtmClient);
            xa.Gid = values[Request.GID];
            xa.TransType = values[Request.TRANS_TYPE];
            xa.Dtm = values.ContainsKey(Request.DTM) ? values[Request.DTM] : string.Empty;
            xa.BranchIDGen = new BranchIDGen(values[Request.BRANCH_ID]);
            xa.Op = values[Request.OP];
            xa.Phase2Url = values.ContainsKey("phase2_url") ? values["phase2_url"] : string.Empty;
            return xa;
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
