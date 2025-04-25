using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using busi;
using Grpc.Core;
using Xunit.Abstractions;

namespace Dtmgrpc.IntegrationTests;

public class MyGrpcProcesser(AsyncDuplexStreamingCall<StreamRequest, StreamReply> call, ITestOutputHelper testOutputHelper)
{
    private TaskCompletionSource _callDisposed = new TaskCompletionSource();
    private readonly ConcurrentDictionary<OperateType, TaskCompletionSource<Status>> progress = new();

    public Task HandleResponse()
    {
        IAsyncEnumerable<StreamReply> asyncEnumerable = call.ResponseStream.ReadAllAsync();
        Task readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in asyncEnumerable)
                {
                    testOutputHelper.WriteLine($"{response.OperateType}: {response.Message}");
                    if (progress.TryGetValue(response.OperateType, out var tcs))
                    {
                        tcs.TrySetResult(new Status(StatusCode.OK, ""));
                    }
                    else
                    {
                        progress[response.OperateType] = new TaskCompletionSource<Status>(new Status(StatusCode.OK, ""));
                    }
                }
            }
            catch (RpcException ex)
            {
                testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");
                if (progress.TryGetValue(OperateType.Try, out var tcs))
                {
                    bool _ = tcs.TrySetResult(ex.Status);
                }
                else
                    progress[OperateType.Try] = new TaskCompletionSource<Status>(ex.Status); // TODO 应答对应的哪个请求

                _callDisposed.SetResult();
                throw;
            }
            catch (Exception ex)
            {
                _callDisposed.SetResult();
                throw;
            }
        });
        return readTask;
    }

    public async Task<Status> GetResult(OperateType operateType)
    {
        if (!progress.TryGetValue(operateType, out var tcs))
        {
            tcs = new TaskCompletionSource<Status>();
            progress[operateType] = tcs;
        }

        Task.WaitAny(_callDisposed.Task, tcs.Task);
        return await tcs.Task;
    }
}