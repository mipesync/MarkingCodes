using System.Net.Sockets;
using System.Text;
using MarkingCodes.Shared;
using MarkingCodes.Client.Options;
using MarkingCodes.Client.Serices.Interfaces;
using Microsoft.Extensions.Options;

namespace MarkingCodes.Client.Workers;

public sealed class TcpClientWorker : BackgroundService
{
    private readonly ILogger<TcpClientWorker> _logger;
    private readonly IMarkingCodeWriter _codeWriter;
    private readonly TcpClientOptions _options;

    public TcpClientWorker(ILogger<TcpClientWorker> logger, IMarkingCodeWriter codeWriter, IOptions<TcpClientOptions> options)
    {
        _logger = logger;
        _codeWriter = codeWriter;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var reconnectDelay = _options.ReconnectInitialDelayMs;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReceiveAsync(cancellationToken);
                reconnectDelay = _options.ReconnectInitialDelayMs;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TCP connection interrupted.");
            }

            if (cancellationToken.IsCancellationRequested)
                break;

            _logger.LogInformation("Reconnecting in {Delay} ms...", reconnectDelay);

            await Task.Delay(reconnectDelay, cancellationToken);

            reconnectDelay = (int)Math.Min((long)reconnectDelay * 2, _options.ReconnectMaxDelayMs);
        }

        _logger.LogInformation("TCP Client stopped.");
    }

    private async Task ConnectAndReceiveAsync(CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        client.NoDelay = true;

        await client.ConnectAsync(_options.Host, _options.Port, cancellationToken);

        _logger.LogInformation("Connected to {Host}:{Port}", _options.Host, _options.Port);

        var stream = client.GetStream();

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.AutoFlush = true;

        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await reader.ReadLineAsync(cancellationToken);

            if (message is null)
            {
                _logger.LogWarning("Server disconnected.");
                return;
            }

            if (!MessageProtocol.TryParseDataMessage(message, out var id, out var code))
            {
                _logger.LogWarning("Invalid message received: {Message}", message);
                continue;
            }

            await _codeWriter.WriteAsync(id, code, cancellationToken);

            var ack = MessageProtocol.CreateAckMessage(id);
            
            await writer.WriteLineAsync(ack.AsMemory(), cancellationToken);

            _logger.LogInformation("Message processed and acknowledged. ID: {Id}", id);
        }
    }
}