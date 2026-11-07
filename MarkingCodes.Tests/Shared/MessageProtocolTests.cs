using MarkingCodes.Shared;

namespace MarkingCodes.Tests.Shared;

public sealed class MessageProtocolTests
{
    [Fact]
    public void CreateDataMessage_ShouldReturnCorrectFormat()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string code = "MARKING_CODE";

        // Act
        var result = MessageProtocol.CreateDataMessage(id, code);

        // Assert
        Assert.Equal($"DATA|{id:N}|{code}", result);
    }

    [Fact]
    public void TryParseDataMessage_ShouldParseValidMessage()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string code = "MARKING_CODE";

        var message = $"DATA|{id:N}|{code}";

        // Act
        var result = MessageProtocol.TryParseDataMessage(message, out var parsedId, out var parsedCode);

        // Assert
        Assert.True(result);
        Assert.Equal(id, parsedId);
        Assert.Equal(code, parsedCode);
    }

    [Fact]
    public void TryParseDataMessage_ShouldPreserveSeparatorsInsideCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string code = "CODE|PART_1|PART_2";

        var message = $"DATA|{id:N}|{code}";

        // Act
        var result = MessageProtocol.TryParseDataMessage(message, out var parsedId, out var parsedCode);

        // Assert
        Assert.True(result);
        Assert.Equal(id, parsedId);
        Assert.Equal(code, parsedCode);
    }

    [Fact]
    public void CreateAckMessage_ShouldReturnCorrectFormat()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = MessageProtocol.CreateAckMessage(id);

        // Assert
        Assert.Equal($"ACK|{id:N}", result);
    }

    [Fact]
    public void TryParseAckMessage_ShouldParseValidMessage()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = MessageProtocol.TryParseAckMessage(
            $"ACK|{id:N}",
            out var parsedId);

        // Assert
        Assert.True(result);
        Assert.Equal(id, parsedId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("DATA")]
    [InlineData("DATA|INVALID|CODE")]
    [InlineData("DATA|550e8400-e29b-41d4-a716-446655440000|CODE")]
    [InlineData("DATA|550e8400e29b41d4a716446655440000|")]
    public void TryParseDataMessage_ShouldRejectInvalidMessages(
        string message)
    {
        // Act
        var result = MessageProtocol.TryParseDataMessage(message, out _, out _);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("ACK")]
    [InlineData("ACK|INVALID")]
    [InlineData("ACK|550e8400-e29b-41d4-a716-446655440000")]
    public void TryParseAckMessage_ShouldRejectInvalidMessages(
        string message)
    {
        // Act
        var result = MessageProtocol.TryParseAckMessage(message, out _);

        // Assert
        Assert.False(result);
    }
}