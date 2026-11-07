namespace MarkingCodes.Client.Serices.Interfaces;

public interface IMarkingCodeWriter
{
    Task WriteAsync(Guid id, string code, CancellationToken cancellationToken);
}