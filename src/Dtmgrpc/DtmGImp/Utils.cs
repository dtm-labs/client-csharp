using DtmCommon;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Linq;

namespace Dtmgrpc.DtmGImp
{
    public static class Utils
    {
        internal static readonly string HTTP = "http://";
        internal static readonly string HTTPS = "https://";
        internal static readonly char Slash = '/';

        internal static string ToJsonString(object obj)
        { 
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }

        internal static TransBase TransBaseFromGrpc(ServerCallContext context)
        {
            var gid = DtmGet(context, Constant.Md.Gid);
            var transType = DtmGet(context, Constant.Md.TransType);
            var dtm = DtmGet(context, Constant.Md.Dtm);
            var branchId = DtmGet(context, Constant.Md.BranchId);
            var op = DtmGet(context, Constant.Md.Op);

            var tb = TransBase.NewTransBase(gid, transType, dtm, branchId);
            tb.Op = op;

            return tb;
        }

        internal static string DtmGet(ServerCallContext context, string key)
        {
            var metadataEntry = context.RequestHeaders?.FirstOrDefault(m => m.Key.Equals(key)) ?? default;

            if (metadataEntry == null || metadataEntry.Equals(default(Metadata.Entry)) || metadataEntry.Value == null)
            {
                return null;
            }

            return metadataEntry.Value;
        }

        internal static Metadata TransInfo2Metadata(string gid, string transType, string branchId, string op, string dtm)
        {
            var metadata = new Metadata();
            metadata.Add(Constant.Md.Gid, gid);
            metadata.Add(Constant.Md.TransType, transType);
            metadata.Add(Constant.Md.BranchId, branchId);
            metadata.Add(Constant.Md.Op, op);
            metadata.Add(Constant.Md.Dtm, dtm);

            return metadata;
        }

        internal static Method<TRequest, TResponse> CreateMethod<TRequest, TResponse>(MethodType methodType, string serviceName, string methodName)
            where TRequest : class, IMessage, new()
            where TResponse : class, IMessage, new()
        {
            return new Method<TRequest, TResponse>(
                methodType,
                serviceName,
                methodName,
                CreateMarshaller<TRequest>(),
                CreateMarshaller<TResponse>());
        }

        internal static Marshaller<TMessage> CreateMarshaller<TMessage>()
              where TMessage : class, IMessage, new()
        {
            return new Marshaller<TMessage>(
                m => m.ToByteArray(),
                d =>
                {
                    var m = new TMessage();
                    m.MergeFrom(d);
                    return m;
                });
        }

        internal static string GetWithoutPrefixgRPCUrl(this string url)
        {
#if NETSTANDARD2_0
            return url?.TrimEnd(Slash)?.Replace(HTTP, string.Empty)?.Replace(HTTPS, string.Empty) ?? string.Empty;
#else
            return url?.TrimEnd(Slash)
                .Replace(HTTP, string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(HTTPS, string.Empty, StringComparison.OrdinalIgnoreCase) ?? string.Empty;
#endif
        }

        private static readonly System.Collections.Generic.Dictionary<string, Exception> StrExceptions = new System.Collections.Generic.Dictionary<string, Exception>
        {
            { Constant.ResultFailure, new DtmFailureException() },
            { Constant.ResultOngoing, new DtmOngingException() },
            { Constant.ResultSuccess, null },
            { string.Empty, null },
        };

        public static Exception String2DtmError(string str)
        {
            return StrExceptions.TryGetValue(str, out var exception) ? exception : null;
        }

        public static Exception GrpcError2DtmError(Exception ex)
        {
            if (ex is RpcException rpcEx)
            {
                if (rpcEx.StatusCode == StatusCode.Aborted)
                {
                    if (rpcEx.Message.Equals(Constant.ResultOngoing))
                    {
                        return new DtmOngingException();
                    }

                    return new DtmFailureException();
                }
                else if (rpcEx.StatusCode == StatusCode.FailedPrecondition)
                {
                    return new DtmOngingException();
                }
            }

            return ex;
        }

        public static RpcException DtmError2GrpcError(Exception ex)
        {
            if (ex is DtmFailureException)
            {
                throw new RpcException(new Status(StatusCode.Aborted, Constant.ResultFailure));
            }
            else if (ex is DtmOngingException)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, Constant.ResultOngoing));
            }

            throw new RpcException(new Status(StatusCode.Unknown, "normal exception"), ex.Message);
        }
    }
}