using DtmCommon;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public interface IDtmClient
    {
        Task<string> GenGid(CancellationToken cancellationToken);

        Task<bool> TransCallDtm(TransBase tb, object body, string operation, CancellationToken cancellationToken);

        Task<bool> TransRegisterBranch(TransBase tb, Dictionary<string, string> added, string operation, CancellationToken cancellationToken);

        Task<HttpResponseMessage> TransRequestBranch(TransBase tb, HttpMethod method, object body, string branchID, string op, string url, CancellationToken cancellationToken);
    }
}
