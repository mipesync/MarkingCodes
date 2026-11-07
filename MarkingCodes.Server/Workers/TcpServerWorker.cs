using System.Net;
using System.Net.Sockets;
using System.Text;
using MarkingCodes.Shared;
using MarkingCodes.Server.Options;
using MarkingCodes.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MarkingCodes.Server.Workers;

public sealed class TcpServerWorker : BackgroundService
{
    private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<TcpServerWorker> _logger;
    private readonly IMarkingCodeGenerator _codeGenerator;
    private readonly TcpServerOptions _options;

    private TcpListener? _listener;

    public TcpServerWorker(ILogger<TcpServerWorker> logger, IMarkingCodeGenerator codeGenerator, IOptions<TcpServerOptions> options)
    {
        _logger = logger;
        _codeGenerator = codeGenerator;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _listener = new TcpListener(IPAddress.Parse(_options.Address), _options.Port);

        var pendingId = Guid.Empty;
        string? pendingCode = null;

        try
        {
            _listener.Start();

            _logger.LogInformation("TCP Server started on {Address}:{Port}", _options.Address, _options.Port);

            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Waiting for client connection...");

                using var client = await _listener.AcceptTcpClientAsync(cancellationToken);

                client.NoDelay = true;

                _logger.LogInformation("Client connected: {Endpoint}", client.Client.RemoteEndPoint);

                await HandleClientAsync(client,
                    () =>
                    {
                        if (pendingId == Guid.Empty)
                        {
                            pendingId = Guid.NewGuid();
                            pendingCode = _codeGenerator.Generate();
                        }

                        return (pendingId, pendingCode!);
                    },
                    async () =>
                    {
                        pendingId = Guid.Empty;
                        pendingCode = null;

                        await Task.Delay(_options.GenerationIntervalMs, cancellationToken);
                    }, cancellationToken);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Нормальное завершение.
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "TCP Server socket error.");
        }
        finally
        {
            _listener.Stop();
            _logger.LogInformation("TCP Server stopped.");
        }
    }

    private async Task HandleClientAsync(TcpClient client, Func<(Guid Id, string Code)> getPendingMessage,
        Func<Task> confirmMessageAsync, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.AutoFlush = true;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var pending = getPendingMessage();

                var message = MessageProtocol.CreateDataMessage(pending.Id, pending.Code);

                await writer.WriteLineAsync(message.AsMemory(), cancellationToken);

                _logger.LogInformation("Message sent. ID: {Id}, Code: {Code}", pending.Id, pending.Code);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(AckTimeout);

                string? response;

                try
                {
                    response = await reader.ReadLineAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("ACK timeout. Retrying message {Id}.", pending.Id);
                    continue;
                }

                if (response is null)
                {
                    _logger.LogWarning("Client disconnected. Pending message: {Id}", pending.Id);
                    return;
                }

                if (!MessageProtocol.TryParseAckMessage(response, out var ackId))
                {
                    _logger.LogWarning("Invalid ACK received: {Response}", response);
                    continue;
                }

                if (ackId != pending.Id)
                {
                    _logger.LogWarning("Unexpected ACK ID: {AckId}. Expected: {ExpectedId}", ackId, pending.Id);
                    continue;
                }

                _logger.LogInformation("Message {Id} successfully acknowledged.", pending.Id);
                await confirmMessageAsync();
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Client connection interrupted.");
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Client socket error.");
        }
    }
}