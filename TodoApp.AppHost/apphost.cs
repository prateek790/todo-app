#:sdk Aspire.AppHost.Sdk@13.6.4
#:package Aspire.Hosting.JavaScript@*
#:package Aspire.Hosting.Redis@*
#:property AspireUseCliBundle=true

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECSHARPAPPS001
var api = builder.AddCSharpApp("api", "../api/api.csproj", options =>
{
    options.LaunchProfileName = "aspire";
});
#pragma warning restore ASPIRECSHARPAPPS001
var redis = builder.AddRedis("redis");
api.WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.AddViteApp("ui", "../todo-app", runScriptName: "start")
    .WithReference(api)
    .WithEnvironment("PORT", "4200")
    .WithEnvironment("API_HTTPS", api.GetEndpoint("https"))
    .WithEnvironment("NG_ALLOWED_HOSTS", "localhost,127.0.0.1")
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
