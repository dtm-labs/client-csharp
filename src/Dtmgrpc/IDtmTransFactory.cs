using System;

namespace Dtmgrpc
{
    public interface IDtmTransFactory
    {
        SagaGrpc NewSagaGrpc(string gid);

        MsgGrpc NewMsgGrpc(string gid);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="nextCronTime">The desired execution time, which can be used to delay downstream consumption</param>
        /// <returns></returns>
        MsgGrpc NewMsgGrpc(string gid, DateTime nextCronTime);

        TccGrpc NewTccGrpc(string gid);
    }
}
