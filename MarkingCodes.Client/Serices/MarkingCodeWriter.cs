using System.Text;
using MarkingCodes.Client.Options;
using MarkingCodes.Client.Serices.Interfaces;
using Microsoft.Extensions.Options;

namespace MarkingCodes.Client.Serices;

public sealed class MarkingCodeFileWriter : IMarkingCodeWriter
{
    private readonly string _filePath;
    private readonly HashSet<Guid> _processedIds = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public MarkingCodeFileWriter(IOptions<TcpClientOptions> options)
    {
        _filePath = Path.GetFullPath(options.Value.OutputFilePath);
        LoadProcessedIds();
    }

    public async Task WriteAsync(Guid id, string code, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_processedIds.Contains(id))
                return;

            var directory = Path.GetDirectoryName(_filePath);
            if (directory is not null)
                Directory.CreateDirectory(directory);

            await using var stream = new FileStream(
                _filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));

            await writer.WriteLineAsync($"{id:N}|{code}".AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);

            _processedIds.Add(id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void LoadProcessedIds()
    {
        if (!File.Exists(_filePath))
            return;

        foreach (var line in File.ReadLines(_filePath))
        {
            var separatorIndex = line.IndexOf('|');
            if (separatorIndex <= 0)
                continue;

            var idPart = line[..separatorIndex];
            if (Guid.TryParseExact(idPart, "N", out var id))
                _processedIds.Add(id);
        }
    }
}