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

        Task TransCallDtm(TransBase tb, object body, string operation, CancellationToken cancellationToken);

        Task TransRegisterBranch(TransBase tb, Dictionary<string, string> added, string operation, CancellationToken cancellationToken);

        Task<HttpResponseMessage> TransRequestBranch(TransBase tb, HttpMethod method, object body, string branchID, string op, string url, CancellationToken cancellationToken);

        Task<HttpResponseMessage> PrepareWorkflow(TransBase tb, CancellationToken cancellationToken);
        
        HttpClient GetHttpClient(string name);

#if NET5_0_OR_GREATER
        TransBase TransBaseFromQuery(Microsoft.AspNetCore.Http.IQueryCollection query);
#endif
        
        Task<TransGlobal> Query(string gid, CancellationToken cancellationToken);
        
        Task<string> QueryStatus(string gid, CancellationToken cancellationToken);
    }
}
