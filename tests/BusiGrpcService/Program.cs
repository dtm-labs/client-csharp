using BusiGrpcService.Services;
using Dtmgrpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS.
    options.ListenLocalhost(5005, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Services.AddDtmGrpc(x =>
{
    x.DtmGrpcUrl = "http://localhost:36790";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<BusiApiService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
