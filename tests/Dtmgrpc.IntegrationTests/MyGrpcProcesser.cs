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
                    TaskCompletionSource<Status> tcs = progress.GetOrAdd(response.OperateType, type => new TaskCompletionSource<Status>());
                    tcs.SetResult(new Status(StatusCode.OK, ""));
                }
            }
            catch (RpcException ex)
            {
                testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");

                // TODO which request does the response correspond to?
                var tcs = progress.GetOrAdd(OperateType.Try, type => new TaskCompletionSource<Status>());
                tcs.SetResult(ex.Status);

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
        TaskCompletionSource<Status> tcs = progress.GetOrAdd(operateType, type => new TaskCompletionSource<Status>());

        Task.WaitAny(_callDisposed.Task, tcs.Task);
        return await tcs.Task;
    }
}