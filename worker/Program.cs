using System.Text.Json;
using DigitalAmnesia.Worker.Models;
using DigitalAmnesia.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var options = WorkerOptions.FromEnvironment();

builder.Services.AddSingleton(options);
builder.Services.AddHttpClient<WorkerApiClient>(client =>
{
    client.BaseAddress = new Uri(options.BackendApiUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigureHttpClient(client =>
{
    client.DefaultRequestVersion = new Version(2, 0);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
});
builder.Services.Configure<JsonSerializerOptions>(serializerOptions =>
{
    serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddHostedService<ScanWorkerService>();

await builder.Build().RunAsync();
