namespace MarkingCodes.Client.Options;

public class TcpClientOptions
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string OutputFilePath { get; set; }
    public required int ReconnectInitialDelayMs { get; set; }
    public required int ReconnectMaxDelayMs { get; set; }
}