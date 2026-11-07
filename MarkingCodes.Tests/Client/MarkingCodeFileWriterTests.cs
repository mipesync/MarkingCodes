using MarkingCodes.Client.Options;
using MarkingCodes.Client.Serices;
using Microsoft.Extensions.Options;

namespace MarkingCodes.Tests.Client;

public sealed class MarkingCodeFileWriterTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public MarkingCodeFileWriterTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _filePath = Path.Combine(_directory, "marking-codes.txt");
    }

    private MarkingCodeFileWriter CreateWriter()
    {
        var options = Options.Create(new TcpClientOptions
        {
            OutputFilePath = _filePath,
            Host = "0.0.0.0",
            Port = 0,
            ReconnectInitialDelayMs = 0,
            ReconnectMaxDelayMs = 0
        });

        return new MarkingCodeFileWriter(options);
    }

    [Fact]
    public async Task WriteAsync_ShouldCreateFile()
    {
        // Arrange
        var writer = CreateWriter();

        var id = Guid.NewGuid();
        const string code = "MARKING_CODE";

        // Act
        await writer.WriteAsync(id, code, CancellationToken.None);

        // Assert
        Assert.True(File.Exists(_filePath));

        var lines = await File.ReadAllLinesAsync(_filePath);

        Assert.Single(lines);
        Assert.Equal($"{id:N}|{code}", lines[0]);
    }

    [Fact]
    public async Task WriteAsync_ShouldAppendMultipleCodes()
    {
        // Arrange
        var writer = CreateWriter();

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        // Act
        await writer.WriteAsync(firstId, "CODE_1", CancellationToken.None);
        await writer.WriteAsync(secondId, "CODE_2", CancellationToken.None);

        // Assert
        var lines = await File.ReadAllLinesAsync(_filePath);

        Assert.Equal(2, lines.Length);
        Assert.Equal($"{firstId:N}|CODE_1", lines[0]);
        Assert.Equal($"{secondId:N}|CODE_2", lines[1]);
    }

    [Fact]
    public async Task WriteAsync_ShouldNotWriteDuplicateMessages()
    {
        // Arrange
        var writer = CreateWriter();

        var id = Guid.NewGuid();

        // Act
        await writer.WriteAsync(id, "CODE_1", CancellationToken.None);
        await writer.WriteAsync(id, "CODE_1", CancellationToken.None);

        // Assert
        var lines = await File.ReadAllLinesAsync(_filePath);
        Assert.Single(lines);
    }

    [Fact]
    public async Task WriteAsync_ShouldRememberProcessedIdsAfterRestart()
    {
        // Arrange
        var id = Guid.NewGuid();

        var firstWriter = CreateWriter();
        await firstWriter.WriteAsync(id, "CODE_1", CancellationToken.None);
        
        var secondWriter = CreateWriter();

        // Act
        await secondWriter.WriteAsync(id, "CODE_1", CancellationToken.None);

        // Assert
        var lines = await File.ReadAllLinesAsync(_filePath);
        Assert.Single(lines);
    }

    [Fact]
    public async Task WriteAsync_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var writer = CreateWriter();

        // Act
        await writer.WriteAsync(Guid.NewGuid(), "CODE_1", CancellationToken.None);

        // Assert
        Assert.True(Directory.Exists(_directory));
        Assert.True(File.Exists(_filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}