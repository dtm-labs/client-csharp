using DtmCommon;
using System;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public delegate Task WfPhase2Func(BranchBarrier bb);

    public delegate Exception WfFunc(Workflow wf, byte[] data);

    public delegate Task<byte[]> WfFunc2(Workflow wf, byte[] data);
}
