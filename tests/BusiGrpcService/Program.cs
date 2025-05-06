using BusiGrpcService.Services;
using Dtmgrpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS.
    options.ListenLocalhost(5005, o => o.Protocols = HttpProtocols.Http2);
    // test for workflow http branch
    options.ListenLocalhost(5006, o => o.Protocols = HttpProtocols.Http1);
});

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddDtmGrpc(x =>
{
    x.DtmGrpcUrl = "http://localhost:36790";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<BusiApiService>();

IWebHostEnvironment env = app.Environment;
if (env.IsDevelopment())
    app.MapGrpcReflectionService();

// test for workflow http branch
app.MapGet("/test-http-ok1", context =>
{
    Console.Out.WriteLine($"QueryString: {context.Request.QueryString}");
    context.Response.StatusCode = 200;
    return context.Response.WriteAsync("SUCCESS"); // FAILURE
});

app.MapGet("/test-http-ok2", context =>
{
    Console.Out.WriteLine($"QueryString: {context.Request.QueryString}");
    context.Response.StatusCode = 200;
    return context.Response.WriteAsync("SUCCESS"); // FAILURE
});
app.MapGet("/409", context =>
{
    Console.Out.WriteLine($"QueryString: {context.Request.QueryString}");
    context.Response.StatusCode = 409;
    return context.Response.WriteAsync("i am body, the http branch is 409"); // FAILURE
});
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();