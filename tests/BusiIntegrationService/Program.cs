using BusiIntegrationService;
using BusiIntegrationService.Services;
using Dtmcli;
using Dtmgrpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Enable HTTP/2 support for unencrypted HTTP connections (required for gRPC over HTTP)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    // Configure gRPC to allow unencrypted HTTP/2 connections (for local development)
    options.EnableDetailedErrors = true;
});
builder.Services.AddDtmGrpc(x =>
{
    x.DtmGrpcUrl = "http://localhost:36790";
});
builder.Services.AddDtmcli(option =>
{
    option.DtmUrl = "http://localhost:36789";
});

// Add controllers for HTTP API
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<BusiApiService>();
app.MapControllers(); // Map the HTTP API controllers
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();