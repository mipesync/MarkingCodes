using MarkingCodes.Client.Options;
using MarkingCodes.Client.Serices;
using MarkingCodes.Client.Serices.Interfaces;
using MarkingCodes.Client.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<TcpClientOptions>()
    .Bind(builder.Configuration.GetSection("TcpClientOptions"))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Host),
        "TCP server host is required.")
    .Validate(
        options => options.Port is > 0 and <= 65535,
        "Invalid TCP server port.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.OutputFilePath),
        "Output file path is required.")
    .Validate(
        options => options.ReconnectInitialDelayMs > 0,
        "Reconnect initial delay must be greater than zero.")
    .Validate(
        options => options.ReconnectMaxDelayMs >= options.ReconnectInitialDelayMs,
        "Maximum reconnect delay must be greater than or equal to initial delay.")
    .ValidateOnStart();

builder.Services.AddSingleton<IMarkingCodeWriter, MarkingCodeFileWriter>();
builder.Services.AddHostedService<TcpClientWorker>();

var host = builder.Build();

await host.RunAsync();