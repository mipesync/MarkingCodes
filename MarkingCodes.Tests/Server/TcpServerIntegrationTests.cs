using System.Net;
using System.Net.Sockets;
using System.Text;
using MarkingCodes.Server.Options;
using MarkingCodes.Server.Services.Interfaces;
using MarkingCodes.Server.Workers;
using MarkingCodes.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarkingCodes.Tests.Server;

public sealed class TcpServerIntegrationTests
{
    [Fact]
    public async Task Server_ShouldSendMessageAndWaitForAck()
    {
        // Arrange
        var port = GetFreePort();

        var generator = new TestMarkingCodeGenerator();

        var options = Options.Create(new TcpServerOptions
        {
            Address = "127.0.0.1",
            Port = port,
            GenerationIntervalMs = 50
        });

        using var server = new TcpServerWorker(NullLogger<TcpServerWorker>.Instance, generator, options);
        await server.StartAsync(CancellationToken.None);

        try
        {
            using var client = new TcpClient();

            await client.ConnectAsync(IPAddress.Loopback, port);

            var stream = client.GetStream();

            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            writer.AutoFlush = true;

            // Act
            var message = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));

            // Assert
            Assert.NotNull(message);
            Assert.True(MessageProtocol.TryParseDataMessage(message, out var id, out var code));
            Assert.Equal("TEST_CODE", code);

            await writer.WriteLineAsync(MessageProtocol.CreateAckMessage(id));

            var nextMessage = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));

            Assert.NotNull(nextMessage);
            Assert.True(MessageProtocol.TryParseDataMessage(nextMessage, out var nextId, out _));
            Assert.NotEqual(id, nextId);
            Assert.Equal(2, generator.GeneratedCount);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Server_ShouldResendPendingMessageAfterReconnect()
    {
        // Arrange
        var port = GetFreePort();

        var options = Options.Create(new TcpServerOptions
        {
            Address = "127.0.0.1",
            Port = port,
            GenerationIntervalMs = 50
        });

        using var server = new TcpServerWorker(NullLogger<TcpServerWorker>.Instance, new TestMarkingCodeGenerator(), options);

        await server.StartAsync(CancellationToken.None);

        try
        {
            string firstMessage;

            using (var firstClient = new TcpClient())
            {
                await firstClient.ConnectAsync(IPAddress.Loopback, port);

                using var reader = new StreamReader(firstClient.GetStream(), Encoding.UTF8, leaveOpen: true);
                firstMessage = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3)) 
                    ?? throw new InvalidOperationException("Server did not send a message.");
            }

            Assert.True(MessageProtocol.TryParseDataMessage(firstMessage, out var firstId, out _));

            using var secondClient = new TcpClient();
            await secondClient.ConnectAsync(IPAddress.Loopback, port);

            using var secondReader = new StreamReader(secondClient.GetStream(), Encoding.UTF8, leaveOpen: true);

            var secondMessage = await secondReader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));

            // Assert
            Assert.Equal(firstMessage, secondMessage);
            Assert.True(MessageProtocol.TryParseDataMessage(secondMessage!, out var secondId, out _));
            Assert.Equal(firstId, secondId);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class TestMarkingCodeGenerator : IMarkingCodeGenerator
    {
        public int GeneratedCount { get; private set; }

        public string Generate()
        {
            GeneratedCount++;
            return "TEST_CODE";
        }
    }
}