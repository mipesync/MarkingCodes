using System.Text.RegularExpressions;
using MarkingCodes.Server.Services;

namespace MarkingCodes.Tests.Server;

public sealed class MarkingCodeGeneratorTests
{
    private readonly MarkingCodeGenerator _generator = new();

    [Fact]
    public void Generate_ShouldReturnValidGs1Format()
    {
        // Act
        var code = _generator.Generate();

        // Assert
        Assert.Matches(@"^\(01\)\d{14}\(21\)[A-F0-9]{20}$", code);
    }

    [Fact]
    public void Generate_ShouldReturnValidGtinCheckDigit()
    {
        // Act
        var code = _generator.Generate();

        var gtin = Regex.Match(code, @"(?<=\(01\))\d{14}").Value;

        // Assert
        Assert.Equal(14, gtin.Length);

        var sum = 0;

        for (var i = 0; i < 13; i++)
        {
            var digit = gtin[i] - '0';
            sum += digit * (i % 2 == 0 ? 3 : 1);
        }

        var expectedCheckDigit = (10 - sum % 10) % 10;

        Assert.Equal(expectedCheckDigit, gtin[13] - '0');
    }

    [Fact]
    public void Generate_ShouldReturnUniqueCodes()
    {
        // Arrange
        const int count = 1000;

        // Act
        var codes = Enumerable
            .Range(0, count)
            .Select(_ => _generator.Generate())
            .ToList();

        // Assert
        Assert.Equal(count, codes.Distinct().Count());
    }
}