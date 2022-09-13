using BusiGrpcService.Services;
using Dtmworkflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS.
    options.ListenLocalhost(5005, o => o.Protocols = HttpProtocols.Http2);
    options.ListenLocalhost(5006, o => o.Protocols = HttpProtocols.Http1);
});

builder.Services.AddGrpc();
builder.Services.AddDtmWorkflow(x =>
{
    x.DtmUrl = "http://localhost:36789";
    x.DtmGrpcUrl = "http://localhost:36790";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<BusiApiService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapPost("/api/busi/workflow/resume", async (HttpRequest req, [FromServices] WorlflowGlobalTransaction wfgt) => 
{
    using (var reader = new StreamReader(req.Body))
    {
        var body = await reader.ReadToEndAsync();
        var bytes = System.Text.Encoding.UTF8.GetBytes(body);
        await wfgt.ExecuteByQS(req.Query, bytes, "");
        return Results.Ok("ok");
    }
    
});
app.MapPost("/api/busi/TransIn", (HttpRequest req, [FromBody] BusiReqDto dto) => { return HandleGeneralBusiness(req, dto.TransInResult, dto.TransOutResult, "TransIn"); });
app.MapPost("/api/busi/TransOut", (HttpRequest req, [FromBody] BusiReqDto dto) => { return HandleGeneralBusiness(req, dto.TransInResult, dto.TransOutResult, "TransOut"); });
app.MapPost("/api/busi/TransInConfirm", (HttpRequest req, [FromBody] BusiReqDto dto) => { return HandleGeneralBusiness(req, dto.TransInResult, dto.TransOutResult, "TransInConfirm"); });
app.MapPost("/api/busi/TransOutConfirm", (HttpRequest req, [FromBody] BusiReqDto dto) => { return HandleGeneralBusiness(req, dto.TransInResult, dto.TransOutResult, "TransOutConfirm"); });
app.MapPost("/api/busi/TransInRevert", (HttpRequest req, [FromBody] BusiReqDto dto) => { return HandleGeneralBusiness(req, dto.TransInResult, dto.TransOutResult, "TransInRevert"); });
app.MapPost("/api/busi/TransOutRevert", (HttpRequest req, [FromBody] BusiReqDto dto) => { return HandleGeneralBusiness(req, dto.TransInResult, dto.TransOutResult, "TransOutRevert"); });
app.MapGet("/api/busi/QueryPrepared", () => { });
app.MapGet("/api/busi/QueryPreparedB", () => { });
app.MapGet("/api/busi/RedisQueryPrepared", () => { });
app.MapGet("/api/busi/MongoQueryPrepared", () => { });

app.Run();

IResult HandleGeneralBusiness(HttpRequest request, string inRes, string outRes, string busi)
{
    Console.WriteLine(busi);
    var res = DtmCommon.Constant.ResultSuccess;

    if (!string.IsNullOrWhiteSpace(inRes))
    {
        res = inRes;
    }
    else if(!string.IsNullOrWhiteSpace(outRes))
    {
        res = outRes;
    }

    if (res == "ERROR") throw new Exception("ERROR from user");

    if (res == DtmCommon.Constant.ResultFailure)
    {
        throw new DtmCommon.DtmFailureException("failure");
    }

    return Results.Ok(res);
}


public class BusiReqDto
{
    public long Amount { get; set; }

    public string TransOutResult { get; set; }

    public string TransInResult { get; set; }
}