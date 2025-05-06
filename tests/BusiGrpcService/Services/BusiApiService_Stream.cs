using System.Text;
using System.Text.Json;
using System.Transactions;
using busi;
using DtmCommon;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace BusiGrpcService.Services;

public partial class BusiApiService
{
    public override async Task StreamTransOutTcc(IAsyncStreamReader<StreamRequest> requestStream, IServerStreamWriter<StreamReply> responseStream, ServerCallContext context)
    {
        // stream try -> confirm/cancel
        await foreach (var request in requestStream.ReadAllAsync())
        {
            BranchBarrier branchBarrier = _barrierFactory.CreateBranchBarrier(
                request.DtmBranchTransInfo.TransType,
                request.DtmBranchTransInfo.Gid,
                request.DtmBranchTransInfo.BranchId,
                request.DtmBranchTransInfo.Op, _logger);
            _logger.LogInformation(
                $"{nameof(StreamTransOutTcc)} gid={branchBarrier.Gid} branch_id={branchBarrier.BranchID} op={branchBarrier.Op}, req={JsonSerializer.Serialize(request)}");

            // using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
            //            new TransactionOptions()
            //            {
            //                IsolationLevel = IsolationLevel.ReadCommitted
            //            },
            //            TransactionScopeAsyncFlowOption.Enabled))
            // {
            //     //DbConnectionManager.GetConnection("TestDB")
            //     await branchBarrier.Call(db: null, async () =>
            //     {
                    switch (request.OperateType)
                    {
                        case OperateType.Try:
                        {
                            if (string.IsNullOrWhiteSpace(request.BusiRequest.TransOutResult) || request.BusiRequest.TransOutResult.Equals("SUCCESS"))
                            {
                                await responseStream.WriteAsync(new StreamReply { OperateType = request.OperateType, Message = "Tried, waiting your confirm..." });
                            }
                            else if (request.BusiRequest.TransOutResult.Equals("FAILURE"))
                            {
                                throw new Grpc.Core.RpcException(new Status(StatusCode.Aborted, "FAILURE"));
                            }
                            else if (request.BusiRequest.TransOutResult.Equals("ONGOING"))
                            {
                                throw new Grpc.Core.RpcException(new Status(StatusCode.FailedPrecondition, "ONGOING"));
                            }
                            else
                            {
                                throw new Grpc.Core.RpcException(new Status(StatusCode.Internal, $"unknow result {request.BusiRequest.TransOutResult}"));
                            }

                            break;
                        }
                        case OperateType.Confirm:
                        {
                            await responseStream.WriteAsync(new StreamReply { OperateType = request.OperateType, Message = "Confirmed" });
                            break;
                        }
                        case OperateType.Cancel:
                        {
                            await responseStream.WriteAsync(new StreamReply { OperateType = request.OperateType, Message = "Canceled" });
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                //     }
                // });
                //
                // scope.Complete();
            }
        }

        _logger.LogInformation($"{nameof(StreamTransOutTcc)} completed");
    }

    public override async Task StreamTransInTcc(IAsyncStreamReader<StreamRequest> requestStream, IServerStreamWriter<StreamReply> responseStream, ServerCallContext context)
    {
        // stream try -> confirm/cancel
        await foreach (var request in requestStream.ReadAllAsync())
        {
            BranchBarrier barrier = _barrierFactory.CreateBranchBarrier(
                request.DtmBranchTransInfo.TransType,
                request.DtmBranchTransInfo.Gid,
                request.DtmBranchTransInfo.BranchId,
                request.DtmBranchTransInfo.Op, _logger);
            _logger.LogInformation(
                $"{nameof(StreamTransOutTcc)} trans_type={barrier.TransType} gid={barrier.Gid} branch_id={barrier.BranchID} op={barrier.Op}, req={JsonSerializer.Serialize(request)}");

            switch (request.OperateType)
            {
                case OperateType.Try:
                {
                    if (string.IsNullOrWhiteSpace(request.BusiRequest.TransInResult) || request.BusiRequest.TransInResult.Equals("SUCCESS"))
                    {
                        await responseStream.WriteAsync(new StreamReply
                        {
                            OperateType = request.OperateType,
                            Message = "Tried, waiting your confirm..."
                        });
                    }
                    else if (request.BusiRequest.TransInResult.Equals("FAILURE"))
                    {
                        throw new Grpc.Core.RpcException(new Status(StatusCode.Aborted, "FAILURE"));
                    }
                    else if (request.BusiRequest.TransInResult.Equals("ONGOING"))
                    {
                        throw new Grpc.Core.RpcException(new Status(StatusCode.FailedPrecondition, "ONGOING"));
                    }
                    else
                    {
                        throw new Grpc.Core.RpcException(new Status(StatusCode.Internal, $"unknow result {request.BusiRequest.TransInResult}"));
                    }

                    break;
                }
                case OperateType.Confirm:
                {
                    await responseStream.WriteAsync(new StreamReply
                    {
                        OperateType = request.OperateType,
                        Message = "Confirmed"
                    });
                    break;
                }
                case OperateType.Cancel:
                {
                    await responseStream.WriteAsync(new StreamReply
                    {
                        OperateType = request.OperateType,
                        Message = "Canceled"
                    });
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _logger.LogInformation($"{nameof(StreamTransInTcc)} completed");
    }
}