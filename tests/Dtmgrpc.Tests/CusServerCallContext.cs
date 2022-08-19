using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmgrpc.Tests
{
    internal class CusServerCallContext : Grpc.Core.ServerCallContext
    {
        private Grpc.Core.Metadata _reqMetadata;

        public CusServerCallContext(Grpc.Core.Metadata reqMetadata)
        {
            this._reqMetadata = reqMetadata;
        }

        protected override string MethodCore => throw new NotImplementedException();

        protected override string HostCore => throw new NotImplementedException();

        protected override string PeerCore => throw new NotImplementedException();

        protected override DateTime DeadlineCore => throw new NotImplementedException();

        protected override Grpc.Core.Metadata RequestHeadersCore => _reqMetadata;

        protected override CancellationToken CancellationTokenCore => throw new NotImplementedException();

        protected override Grpc.Core.Metadata ResponseTrailersCore => throw new NotImplementedException();

        protected override Grpc.Core.Status StatusCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        protected override Grpc.Core.WriteOptions WriteOptionsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override Grpc.Core.AuthContext AuthContextCore => throw new NotImplementedException();

        protected override Grpc.Core.ContextPropagationToken CreatePropagationTokenCore(Grpc.Core.ContextPropagationOptions options)
        {
            throw new NotImplementedException();
        }

        protected override Task WriteResponseHeadersAsyncCore(Grpc.Core.Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }
    }
}
