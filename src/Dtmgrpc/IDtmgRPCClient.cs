using DtmCommon;
using Google.Protobuf;
using Grpc.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmgrpc
{
    public interface IDtmgRPCClient
    {
        Task DtmGrpcCall(TransBase transBase, string operation);

        Task<string> GenGid();

        TransBase TransBaseFromGrpc(ServerCallContext context);

        Task RegisterBranch(TransBase tb, string branchId, ByteString bd, Dictionary<string, string> added, string operation);

        /// <summary>
        /// Invoke branch
        /// </summary>
        /// <typeparam name="TRequest">gRPC request</typeparam>
        /// <typeparam name="TResponse">gRPC response</typeparam>
        /// <param name="tb">trans base</param>
        /// <param name="msg">gRPC request</param>
        /// <param name="url">gRPC url, don't contain http</param>
        /// <param name="branchId">branch id</param>
        /// <param name="op">op</param>
        /// <returns>gRPC response</returns>
        Task<TResponse> InvokeBranch<TRequest, TResponse>(TransBase tb, TRequest msg, string url, string branchId, string op)
            where TRequest : class, IMessage, new()
            where TResponse : class, IMessage, new();
    }
}
