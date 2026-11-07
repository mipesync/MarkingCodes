namespace MarkingCodes.Shared;

public static class MessageProtocol
{
    private const string DataPrefix = "DATA";
    private const string AckPrefix = "ACK";

    public static string CreateDataMessage(Guid id, string code)
    {
        return $"{DataPrefix}|{id:N}|{code}";
    }

    public static string CreateAckMessage(Guid id)
    {
        return $"{AckPrefix}|{id:N}";
    }

    public static bool TryParseDataMessage(string message, out Guid id, out string code)
    {
        id = Guid.Empty;
        code = string.Empty;

        var parts = message.Split('|', 3);
        if (parts.Length != 3 || parts[0] != DataPrefix || !Guid.TryParseExact(parts[1], "N", out id) || string.IsNullOrWhiteSpace(parts[2]))
            return false;

        code = parts[2];
        return true;
    }

    public static bool TryParseAckMessage(string message, out Guid id)
    {
        id = Guid.Empty;

        var parts = message.Split('|', 2);
        return parts.Length == 2 && parts[0] == AckPrefix && Guid.TryParseExact(parts[1], "N", out id);
    }
}