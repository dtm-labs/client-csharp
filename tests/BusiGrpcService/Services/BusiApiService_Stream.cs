using System.Text;
using System.Text.Json;
using System.Transactions;
using busi;
using DtmCommon;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MySqlConnector;

namespace BusiGrpcService.Services;

public partial class BusiApiService
{
    public override async Task StreamTransOutTcc(IAsyncStreamReader<StreamRequest> requestStream, IServerStreamWriter<StreamReply> responseStream, ServerCallContext context)
    {
        // stream try -> confirm/cancel
        await foreach (var request in requestStream.ReadAllAsync())
        {
            if (request.DtmBranchTransInfo != null)
            {
                BranchBarrier branchBarrier = _barrierFactory.CreateBranchBarrier(
                    request.DtmBranchTransInfo.TransType,
                    request.DtmBranchTransInfo.Gid,
                    request.DtmBranchTransInfo.BranchId,
                    request.DtmBranchTransInfo.Op, _logger);
                _logger.LogInformation(
                    $"{nameof(StreamTransOutTcc)} gid={branchBarrier.Gid} branch_id={branchBarrier.BranchID} op={branchBarrier.Op}, req={JsonSerializer.Serialize(request)}");

                await using MySqlConnection conn = GetBarrierConn();
                (bool done, string reason) = await branchBarrier.Call(conn, async () =>
                {
                    // business logic
                    await TransOutFn(responseStream, request);
                });
                if (!done)
                {
                    _logger.LogInformation($"NOT done, reason:{reason} {nameof(StreamTransOutTcc)} gid={branchBarrier.Gid} branch_id={branchBarrier.BranchID} op={branchBarrier.Op}");
                    await responseStream.WriteAsync(new StreamReply { OperateType = request.OperateType, Message = reason });
                }
            }
            else
            {
                await TransOutFn(responseStream, request);
            }
        }

        _logger.LogInformation($"{nameof(StreamTransOutTcc)} completed");
    }

    private static async Task TransOutFn(IServerStreamWriter<StreamReply> responseStream, StreamRequest request)
    {
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
        }
    }

    public override async Task StreamTransInTcc(IAsyncStreamReader<StreamRequest> requestStream, IServerStreamWriter<StreamReply> responseStream, ServerCallContext context)
    {
        // stream try -> confirm/cancel
        await foreach (var request in requestStream.ReadAllAsync())
        {
            if (request.DtmBranchTransInfo != null)
            {
                BranchBarrier branchBarrier = _barrierFactory.CreateBranchBarrier(
                    request.DtmBranchTransInfo.TransType,
                    request.DtmBranchTransInfo.Gid,
                    request.DtmBranchTransInfo.BranchId,
                    request.DtmBranchTransInfo.Op, _logger);
                _logger.LogInformation(
                    $"{nameof(StreamTransInTcc)} gid={branchBarrier.Gid} branch_id={branchBarrier.BranchID} op={branchBarrier.Op}, req={JsonSerializer.Serialize(request)}");

                await using MySqlConnection conn = GetBarrierConn();
                (bool done, string reason) = await branchBarrier.Call(conn, async () =>
                {
                    // business logic
                    await TransInFn(responseStream, request);
                });
                if (!done)
                {
                    _logger.LogInformation($"NOT done, reason:{reason} {nameof(StreamTransInTcc)} gid={branchBarrier.Gid} branch_id={branchBarrier.BranchID} op={branchBarrier.Op}");
                    await responseStream.WriteAsync(new StreamReply { OperateType = request.OperateType, Message = reason });
                }
            }
            else
            {
                await TransInFn(responseStream, request);
            }
        }

        _logger.LogInformation($"{nameof(StreamTransInTcc)} completed");
    }

    private static async Task TransInFn(IServerStreamWriter<StreamReply> responseStream, StreamRequest request)
    {
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
}