using System.Net;
using System.Net.Sockets;
using System.Text;
using MarkingCodes.Shared;
using MarkingCodes.Client.Options;
using MarkingCodes.Client.Serices;
using MarkingCodes.Client.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarkingCodes.Tests.Client;

public sealed class TcpClientIntegrationTests
{
    [Fact]
    public async Task Client_ShouldSaveMessageAndSendAck()
    {
        // Arrange
        var port = GetFreePort();

        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var filePath = Path.Combine(directory, "marking-codes.txt");

        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        
        var options = Options.Create(new TcpClientOptions
        {
            Host = "127.0.0.1",
            Port = port,
            OutputFilePath = filePath,
            ReconnectInitialDelayMs = 1000,
            ReconnectMaxDelayMs = 10000
        });

        var writer = new MarkingCodeFileWriter(options);

        using var clientWorker = new TcpClientWorker(NullLogger<TcpClientWorker>.Instance, writer, options);
        await clientWorker.StartAsync(CancellationToken.None);

        try
        {
            using var serverClient = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(3));

            var stream = serverClient.GetStream();

            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            await using var serverWriter = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            serverWriter.AutoFlush = true;

            var id = Guid.NewGuid();

            const string code = "TEST_MARKING_CODE";

            // Act
            await serverWriter.WriteLineAsync(MessageProtocol.CreateDataMessage(id, code));

            var ack = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));

            // Assert
            Assert.Equal(MessageProtocol.CreateAckMessage(id), ack);

            var lines = await File.ReadAllLinesAsync(filePath);

            Assert.Single(lines);
            Assert.Equal($"{id:N}|{code}", lines[0]);
        }
        finally
        {
            await clientWorker.StopAsync(CancellationToken.None);

            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}