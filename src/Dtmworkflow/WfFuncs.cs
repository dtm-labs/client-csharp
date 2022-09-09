using DtmCommon;
using System;

namespace Dtmworkflow
{
    public delegate Exception WfPhase2Func(BranchBarrier bb);

    internal delegate Exception WfFunc(Workflow wf, byte[] data);

    internal delegate (byte[], Exception) WfFunc2(Workflow wf, byte[] data);
}
