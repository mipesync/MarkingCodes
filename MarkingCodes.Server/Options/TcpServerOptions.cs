namespace MarkingCodes.Server.Options;

public class TcpServerOptions
{
    public required string Address { get; set; }
    public required int Port { get; set; }
    public required int GenerationIntervalMs { get; set; }
}