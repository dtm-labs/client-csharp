using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public interface IDtmClient : IDisposable
    {
        Task<string> GenGid(CancellationToken cancellationToken);

        Task<bool> TransCallDtm(DtmImp.TransBase tb, object body, string operation, CancellationToken cancellationToken);

        Task<bool> TransRegisterBranch(DtmImp.TransBase tb, Dictionary<string, string> added, string operation, CancellationToken cancellationToken);

        Task<HttpResponseMessage> TransRequestBranch(DtmImp.TransBase tb, object body, string branchID, string op, string url, CancellationToken cancellationToken);
    }
}
