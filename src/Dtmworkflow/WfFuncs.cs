using DtmCommon;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    /// <summary>
    /// WfPhase2Func is the type for phase 2 function
    /// </summary>
    /// <param name="bb"></param>
    /// <returns></returns>
    public delegate Task WfPhase2Func(BranchBarrier bb);

    /// <summary>
    /// WfFunc is the type for workflow function
    /// </summary>
    /// <param name="wf"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public delegate Task WfFunc(Workflow wf, byte[] data);

    /// <summary>
    /// WfFunc2 is the type for workflow function with return value
    /// </summary>
    /// <param name="wf"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public delegate Task<byte[]> WfFunc2(Workflow wf, byte[] data);
}