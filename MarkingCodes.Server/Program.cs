using System.Net;
using MarkingCodes.Server.Options;
using MarkingCodes.Server.Services;
using MarkingCodes.Server.Services.Interfaces;
using MarkingCodes.Server.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<TcpServerOptions>()
    .Bind(builder.Configuration.GetSection("TcpServerOptions"))
    .Validate(
        options => IPAddress.TryParse(options.Address, out _),
        "Invalid TCP server address.")
    .Validate(
        options => options.Port is > 0 and <= 65535,
        "Invalid TCP server port.")
    .Validate(
        options => options.GenerationIntervalMs > 0,
        "Generation interval must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSingleton<IMarkingCodeGenerator, MarkingCodeGenerator>();
builder.Services.AddHostedService<TcpServerWorker>();

var host = builder.Build();

await host.RunAsync();