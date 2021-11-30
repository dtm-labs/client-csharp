using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public interface IDtmClient : IDisposable
    {
        Task<bool> RegisterTccBranch(RegisterTccBranch  registerTcc, CancellationToken cancellationToken);

        Task<bool> TccPrepare(TccBody tccBody, CancellationToken cancellationToken);


        Task<DtmResult> TccSubmit(TccBody tccBody, CancellationToken cancellationToken);


        Task<bool> TccAbort(TccBody tccBody, CancellationToken cancellationToken);


        Task<string> GenGid(CancellationToken cancellationToken);
    }
}
