using DtmCommon;
using Dtmgrpc.DtmGImp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmgrpc
{
    public class DtmgRPCClient : IDtmgRPCClient
    {
        private static readonly Marshaller<dtmgpb.DtmRequest> DtmRequestMarshaller = Marshallers.Create(r => r.ToByteArray(), data => dtmgpb.DtmRequest.Parser.ParseFrom(data));
        private static readonly Marshaller<Empty> DtmReplyMarshaller = Marshallers.Create(r => r.ToByteArray(), data => Empty.Parser.ParseFrom(data));
        private static readonly string DtmServiceName = "dtmgimp.Dtm";
        private static readonly string HTTP = "http";
        private static readonly string HTTPPrefix = "http://";

        private readonly Driver.IDtmDriver _dtmDriver;
        private readonly DtmOptions _options;

        public DtmgRPCClient(Driver.IDtmDriver dtmDriver, IOptions<DtmOptions> optionsAccs)
        {
            this._dtmDriver = dtmDriver;
            this._options = optionsAccs.Value;
        }

        public async Task DtmGrpcCall(TransBase transBase, string operation)
        {
            var dtmRequest = BuildDtmRequest(transBase);
            var method = new Method<dtmgpb.DtmRequest, Empty>(MethodType.Unary, DtmServiceName, operation, DtmRequestMarshaller, DtmReplyMarshaller);

            using var channel = GrpcChannel.ForAddress(_options.DtmGrpcUrl);
            var callOptions = new CallOptions()
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(_options.DtmTimeout));
            await channel.CreateCallInvoker().AsyncUnaryCall(method, string.Empty, callOptions, dtmRequest);
        }

        public async Task<string> GenGid()
        {
            using var channel = GrpcChannel.ForAddress(_options.DtmGrpcUrl);
            var client = new dtmgpb.Dtm.DtmClient(channel);
            var callOptions = new CallOptions()
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(_options.DtmTimeout));
            var reply = await client.NewGidAsync(new Empty(), callOptions);
            return reply.Gid;
        }

        public async Task<TResponse> InvokeBranch<TRequest, TResponse>(TransBase tb, TRequest msg, string url, string branchId, string op)
            where TRequest : class, IMessage, new()
            where TResponse : class, IMessage, new()
        {
            var (server, serviceName, method, err) = _dtmDriver.ParseServerMethod(url);

            if (!string.IsNullOrWhiteSpace(err)) throw new DtmException(err);

            if (!server.StartsWith(HTTP, StringComparison.OrdinalIgnoreCase))
            {
                server = $"{HTTPPrefix}{server}";
            }

            using var channel = GrpcChannel.ForAddress(server);
            var grpcMethod = Utils.CreateMethod<TRequest, TResponse>(MethodType.Unary, serviceName, method);

            var metadata = Utils.TransInfo2Metadata(tb.Gid, tb.TransType, branchId, op, tb.Dtm);
            var callOptions = new CallOptions()
                .WithHeaders(metadata)
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(_options.BranchTimeout));
            var resp = await channel.CreateCallInvoker().AsyncUnaryCall(grpcMethod, string.Empty, callOptions, msg);
            return resp;
        }

        public async Task RegisterBranch(TransBase tb, string branchId, ByteString bd, Dictionary<string, string> added, string operation)
        {
            var request = new dtmgpb.DtmBranchRequest
            {
                Gid = tb.Gid,
                TransType = tb.TransType,
            };

            request.BranchID = branchId;
            request.BusiPayload = bd;
            request.Data.Add(added);

            using var channel = GrpcChannel.ForAddress(_options.DtmGrpcUrl);
            var client = new dtmgpb.Dtm.DtmClient(channel);
            var callOptions = new CallOptions()
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(_options.DtmTimeout));
            await client.RegisterBranchAsync(request, callOptions);
        }

        public TransBase TransBaseFromGrpc(ServerCallContext context)
        {
            return Utils.TransBaseFromGrpc(context);
        }

        private dtmgpb.DtmRequest BuildDtmRequest(TransBase transBase)
        {
            var transOptions = new dtmgpb.DtmTransOptions
            {
                WaitResult = transBase.WaitResult,
                TimeoutToFail = transBase.TimeoutToFail,
                RetryInterval = transBase.RetryInterval,
            };

            if (transBase.BranchHeaders != null)
            {
                transOptions.BranchHeaders.Add(transBase.BranchHeaders);
            }

            if (transBase.PassthroughHeaders != null)
            {
                transOptions.PassthroughHeaders.Add(transBase.PassthroughHeaders);
            }

            var dtmRequest = new dtmgpb.DtmRequest
            {
                Gid = transBase.Gid,
                TransType = transBase.TransType,
                TransOptions = transOptions,
                QueryPrepared = transBase.QueryPrepared ?? string.Empty,
                CustomedData = transBase.CustomData ?? string.Empty,
                Steps = transBase.Steps == null ? string.Empty : Utils.ToJsonString(transBase.Steps),
            };

            foreach (var item in transBase.BinPayloads ?? new List<byte[]>())
            {
                dtmRequest.BinPayloads.Add(ByteString.CopyFrom(item));
            }

            return dtmRequest;
        }
    }
}
